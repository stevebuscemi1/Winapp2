﻿'    Copyright (C) 2018-2019 Robbie Ward
' 
'    This file is a part of Winapp2ool
' 
'    Winapp2ool is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.
'
'    Winap2ool is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.
'
'    You should have received a copy of the GNU General Public License
'    along with Winapp2ool.  If not, see <http://www.gnu.org/licenses/>.
Option Strict On
''' <summary>
''' Represents a winapp2.ini format iniFile, and enables easy access to format specific iniFile information
''' </summary>
Public Class winapp2file
    ' "" = main section, bottom most in all circumstances and appearing without a label 
    ''' <summary>The names of the sections of entries as they appear in winapp2.ini</summary>
    Public ReadOnly Property FileSectionHeaders As String() = {"Chrome/Chromium based browsers", "Chromium based Edge", "Firefox/Mozilla based browsers", "Thunderbird", "Opera",
        "Language entries", "Potentially very long scan time (and also dangerous) entries", "Dangerous entries", ""}
    ' As above, index 0 = Chrome, 1 = Chrome Based Edge, 2 = FireFox.... 9 = ""
    ''' <summary>A list of iniFiles each containing one of the headers contents</summary>
    Public Property EntrySections As New List(Of iniFile)
    ''' <summary>The list of winapp2entry objects for each header section</summary>
    Public Property Winapp2entries As New List(Of List(Of winapp2entry))
    ''' <summary>Indicates whether or not this object represents a Non-CCleaner variant of winapp2.ini</summary>
    Public Property IsNCC As Boolean = False
    '''<summary>The directory of the iniFile object used to instantiate this object</summary>
    Public Property Dir As String = ""
    '''<summary>The file name of the iniFile object used to instantiate this object</summary>
    Public Property Name As String = ""
    '''<summary>The version in YYMMDD format of the winapp2.ini file (Defaults to 000000)</summary>
    Public Property Version As String = "000000"

    ''' <summary>Create a new meta winapp2 object from an iniFile object</summary>
    ''' <param name="file">A winapp2.ini format iniFile object</param>
    Public Sub New(ByVal file As iniFile)
        dir = file.Dir
        name = file.Name
        For i As Integer = 0 To 8
            EntrySections.Add(New iniFile With {.Name = FileSectionHeaders(i)})
            Winapp2entries.Add(New List(Of winapp2entry))
        Next
        ' Determine if we're the Non-CCleaner variant of the ini
        IsNCC = Not file.findCommentLine("; This is the non-CCleaner version of Winapp2 that contains extra entries that were removed due to them being added to CCleaner.") = -1
        ' Determine the version string
        If file.Comments.Count = 0 Then version = "; version 000000"
        If file.Comments.Count > 0 Then version = If(Not file.Comments.Values(0).Comment.ToLower.Contains("version"), "; version 000000", file.Comments.Values(0).Comment)
        ' Build the header sections for browsers/Thunderbird/winapp3
        Dim langSecRefs As New List(Of String) From {"3029", "3005", "3026", "3030", "3027", "Language Files", "Dangerous Long", "Dangerous"}
        For Each section In file.Sections.Values
            Dim tmpwa2entry As New winapp2entry(section)
            Dim ind = -1
            If tmpwa2entry.LangSecRef.keyCount > 0 Then
                ind = langSecRefs.IndexOf(tmpwa2entry.LangSecRef.Keys.First.Value)
            ElseIf tmpwa2entry.SectionKey.keyCount > 0 Then
                ind = langSecRefs.IndexOf(tmpwa2entry.SectionKey.Keys.First.Value)
            End If
            If ind = -1 Then ind = 8
            addToInnerFile(ind, tmpwa2entry, section)
        Next
    End Sub

    ''' <summary>Inserts an iniSection into its respective tracking file and records the winapp2entry object form accordingly. </summary>
    ''' <param name="ind">The index of the tracking file</param>
    ''' <param name="entry">The section in winapp2entry format</param>
    ''' <param name="section">A section to be tracked</param>
    Private Sub addToInnerFile(ind As Integer, entry As winapp2entry, section As iniSection)
        If Not entrySections(ind).Sections.Keys.Contains(section.Name) Then
            entrySections(ind).Sections.Add(section.Name, section)
            winapp2entries(ind).Add(entry)
        End If
    End Sub

    ''' <summary>Returns the total number of entries stored in the internal iniFile objects</summary>
    Public Function count() As Integer
        Dim out As Integer = 0
        For Each section In entrySections
            out += section.Sections.Count
        Next
        Return out
    End Function

    ''' <summary>Sorts the internal iniFile objects in winapp2.ini format order</summary>
    Public Sub sortInneriniFiles()
        For Each innerIni In entrySections
            innerIni.sortSections(sortEntryNames(innerIni))
        Next
    End Sub

    ''' <summary>Rebuilds a list of winapp2entry objects back into iniSection objects and returns the collection of them as an iniFile</summary>
    ''' <param name="entryList"></param>
    Private Function rebuildInnerIni(ByRef entryList As List(Of winapp2entry)) As iniFile
        Dim tmpini As New iniFile
        For Each entry In entryList
            tmpini.Sections.Add(entry.Name, New iniSection(entry.dumpToListOfStrings))
            tmpini.Sections.Values.Last.StartingLineNumber = entry.LineNum
        Next
        Return tmpini
    End Function

    ''' <summary>Updates the internal iniFile objects</summary>
    Public Sub rebuildToIniFiles()
        For i As Integer = 0 To entrySections.Count - 1
            entrySections(i) = rebuildInnerIni(winapp2entries(i))
            entrySections(i).Name = FileSectionHeaders(i)
        Next
    End Sub

    ''' <summary>Builds and returns the winapp2.ini text including header comments for writing back to a file</summary>
    Public Function winapp2string() As String
        Dim fileName As String = If(isNCC, "Winapp2 (Non-CCleaner version)", "Winapp2")
        Dim licLink As String = appendNewLine(If(isNCC, "https://github.com/MoscaDotTo/Winapp2/blob/master/Non-CCleaner/License.md", "https://github.com/MoscaDotTo/Winapp2/blob/master/License.md"))
        ' Version string (YYMMDD format) & entry count 
        Dim out As String = appendNewLine(version)
        out += appendNewLine($"; # of entries: {count.ToString("#,###")}")
        out += appendNewLine(";")
        out += $"; {fileName} is fully licensed under the CC-BY-SA-4.0 license agreement. Please refer to our license agreement before using Winapp2: {licLink}"
        out += appendNewLine($"; If you plan on modifying, distributing, and/or hosting {fileName} for your own program or website, please ask first.")
        out += appendNewLine(";")
        If isNCC Then
            out += appendNewLine("; This is the non-CCleaner version of Winapp2 that contains extra entries that were removed due to them being added to CCleaner.")
            out += appendNewLine("; Do not use this file for CCleaner as the extra cleaners may cause conflicts with CCleaner.")
        End If
        out += appendNewLine("; You can get the latest Winapp2 here: https://github.com/MoscaDotTo/Winapp2")
        out += appendNewLine("; Any contributions are appreciated. Please refer to our ReadMe to learn to make your own entries here: https://github.com/MoscaDotTo/Winapp2/blob/master/README.md")
        out += appendNewLine("; Try out Winapp2ool for many useful additional features including updating and trimming winapp2.ini: https://github.com/MoscaDotTo/Winapp2/raw/master/winapp2ool/bin/Release/winapp2ool.exe")
        out += appendNewLine("; You can find the Winapp2ool ReadMe here: https://github.com/MoscaDotTo/Winapp2/blob/master/winapp2ool/Readme.md")
        ' Adds each section's toString if it exists with a proper header and footer, followed by the main section (if it exists)
        For i As Integer = 0 To 5
            If entrySections(i).Sections.Count > 0 Then
                out += appendNewLine("; ")
                out += appendNewLine(appendNewLine("; " & entrySections(i).Name))
                out += entrySections(i).toString
                out += appendNewLine($"{prependNewLines(False)}; End of {entrySections(i).Name}")
            End If
        Next
        If entrySections.Last.Sections.Count > 0 Then out += prependNewLines(False) & entrySections.Last.toString
        Return out
    End Function
End Class