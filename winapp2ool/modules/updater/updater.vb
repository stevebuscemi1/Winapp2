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
Imports System.IO
''' <summary>Holds functions used for checking for and updating winapp2.ini and winapp2ool.exe</summary>
Public Module updater
    ''' <summary>Indicates the latest available verson of winapp2ool from GitHub</summary>
    Public Property latestVersion As String = ""
    ''' <summary>Indicates the latest available version of winapp2.ini from GitHub</summary>
    Public Property latestWa2Ver As String = ""
    ''' <summary>Indicates the local version of winapp2.ini (if available)</summary>
    Public Property localWa2Ver As String = "000000"
    ''' <summary>Indicates that a winapp2ool update is available from GitHub</summary>
    Public Property updateIsAvail As Boolean = False
    ''' <summary>Indicates that a winapp2.ini update is available from GitHub</summary>
    Public Property waUpdateIsAvail As Boolean = False
    ''' <summary>The current version of the executable as used for version checking against GitHub</summary>
    Public ReadOnly Property currentVersion As String = System.Reflection.Assembly.GetExecutingAssembly.FullName.Split(CChar(","))(1).Substring(9)
    ''' <summary>Indicates whether or not winapp2ool has already checked for updates</summary>
    Public Property checkedForUpdates As Boolean = False

    ''' <summary>Checks the versions of winapp2ool, .NET, and winapp2.ini and records if any are outdated.</summary>
    Public Sub checkUpdates()
        If checkedForUpdates Then Exit Sub
        Try
            gLog("Checking for updates")
            ' Query the latest winapp2ool.exe and winapp2.ini versions 
            If isBeta Then
                Dim tmp = Environment.GetEnvironmentVariable("temp")
                download(New iniFile($"{Environment.GetEnvironmentVariable("temp")}\", "`winapp2ool.exe"), toolExeLink, False, True)

                latestVersion = System.Reflection.Assembly.LoadFile($"{Environment.GetEnvironmentVariable("temp")}\winapp2ool.exe").FullName.Split(CChar(","))(1).Substring(9)
                Dim tmp1 = latestVersion.Split(CChar("."))
                ' If the build time is earlier than 2:46am (10000 seconds), the last part of the version number will be one digit short 
                ' Pad it with a 0 when this is the case to avoid telling user's there's an update available when there is not 
                If Not tmp1.Last.Length = 5 Then
                    Dim tmp2 = ""
                    For i = tmp1.Last.Length To 5
                        tmp2 += "0"
                    Next
                    latestVersion = latestVersion.Replace(tmp1.Last, tmp2 & tmp1.Last)
                End If
            Else
                latestVersion = getFileDataAtLineNum(toolVerLink)
            End If
            latestWa2Ver = getFileDataAtLineNum(winapp2link).Split(CChar(" "))(2)
            ' This should only be true if a user somehow has internet but cannot otherwise connect to the GitHub resources used to check for updates
            ' In this instance we should consider the update check to have failed and put the application into offline mode
            If latestVersion = "" Or latestWa2Ver = "" Then updateCheckFailed("online", True) : Exit Try
            ' Observe whether or not updates are available, using val to avoid conversion mistakes
            updateIsAvail = Val(latestVersion.Replace(".", "")) > Val(currentVersion.Replace(".", ""))
            getLocalWinapp2Version()
            waUpdateIsAvail = Val(latestWa2Ver) > Val(localWa2Ver)
            checkedForUpdates = True
            gLog("Update check complete:")
            gLog($"Winapp2ool:")
            gLog("Local: " & currentVersion, indent:=True)
            gLog("Remote: " & latestVersion, indent:=True)
            gLog("Winapp2.ini:")
            gLog("Local:" & localWa2Ver, indent:=True)
            gLog("Remote: " & latestWa2Ver, indent:=True)
        Catch ex As Exception
            exc(ex)
            updateCheckFailed("winapp2ool or winapp2.ini")
        End Try
    End Sub

    ''' <summary>Handles the case where the update check has failed</summary>
    ''' <param name="name">The name of the component whose update check failed</param>
    ''' <param name="chkOnline">A flag specifying that the internet connection should be retested</param>
    Private Sub updateCheckFailed(name As String, Optional chkOnline As Boolean = False)
        setHeaderText($"/!\ {name} update check failed. /!\", True)
        localWa2Ver = "000000"
        If chkOnline Then chkOfflineMode()
    End Sub

    ''' <summary>Attempts to return the version number from the first line of winapp2.ini, returns "000000" if it can't</summary>
    Private Sub getLocalWinapp2Version()
        If Not File.Exists(Environment.CurrentDirectory & "\winapp2.ini") Then localWa2Ver = "000000 (File not found)" : Exit Sub
        Dim localStr = getFileDataAtLineNum(Environment.CurrentDirectory & "\winapp2.ini", remote:=False).ToLower
        If localStr.Contains("version") Then localWa2Ver = localStr.Split(CChar(" "))(2)
    End Sub


    ''' <summary>Updates the offline status of winapp2ool</summary>
    Public Sub chkOfflineMode()
        gLog("Checking online status")
        isOffline = Not checkOnline()
    End Sub

    ''' <summary>Informs the user when an update is available</summary>
    ''' <param name="cond">The update condition</param>
    ''' <param name="updName">The item (winapp2.ini or winapp2ool) for which there is a pending update</param>
    ''' <param name="oldVer">The old (currently in use) version</param>
    ''' <param name="newVer">The updated version pending download</param>
    Public Sub printUpdNotif(cond As Boolean, updName As String, oldVer As String, newVer As String)
        If cond Then
            gLog($"Update available for {updName} from {oldVer} to {newVer}")
            Console.ForegroundColor = ConsoleColor.Green
            print(0, $"A new version of {updName} is available!", isCentered:=True)
            print(0, $"Current  : v{oldVer}", isCentered:=True)
            print(0, $"Available: v{newVer}", trailingBlank:=True, isCentered:=True)
            Console.ResetColor()
        End If
    End Sub

    ''' <summary>Downloads the latest version of winapp2ool.exe and replaces the currently running executable with it before launching that new executable and closing the program.</summary>
    Public Sub autoUpdate()
        gLog("Starting auto update process")
        Dim newTool As New iniFile(Environment.CurrentDirectory, "winapp2ool updated.exe")
        Dim backupName = $"winapp2ool v{currentVersion}.exe.bak"
        Try
            ' Remove any existing backups of this version
            If File.Exists($"{Environment.CurrentDirectory}\{backupName}") Then File.Delete($"{Environment.CurrentDirectory}\{backupName}")
            ' Remove any old update files that didn't get renamed for whatever reason
            If File.Exists(newTool.Path) Then File.Delete(newTool.Path)
            download(newTool, If(isBeta, betaToolLink, toolLink), False)
            ' Rename the executables and launch the new one
            File.Move("winapp2ool.exe", backupName)
            File.Move("winapp2ool updated.exe", "winapp2ool.exe")
            System.Diagnostics.Process.Start($"{Environment.CurrentDirectory}\winapp2ool.exe")
            Environment.Exit(0)
        Catch ex As Exception
            exc(ex)
            File.Move(backupName, "winapp2ool.exe")
        End Try
    End Sub
End Module