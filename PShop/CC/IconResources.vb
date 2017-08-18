Imports System.IO
Imports System.IO.Compression
Imports Newtonsoft.Json.Linq

Namespace PShop.CC
    <Obsolete>
    Public Class IconResources
        Private Const NameSize As Integer = 48
        Private Const DataSize As Integer = 320 ' TODO: 160

        Property IndexFileName As String

        Property Name As String
        Property Version As String
        Property LowResolutionDataFile As String
        Property HighResolutionDataFile As String
        Property Icons As New List(Of ResourceIcon)

        Private MarkerIndex(4) As Integer

        Private Initialized As Boolean = False

        Private IsCC2014 As Boolean = False

        Sub New()

        End Sub

        Sub New(indexFileStram As Stream)
            Initialize(indexFileStram)
        End Sub

        Private Sub Initialize(indexFileStream As Stream)
            Dim buf() As Byte
            Using ms = New MemoryStream
                indexFileStream.CopyTo(ms)
                buf = ms.ToArray
            End Using

            If buf(0) = Convert.ToByte("{"c) Then
                ' CC
                Me.IsCC2014 = False

                Dim o = JObject.Parse(Text.Encoding.ASCII.GetString(buf))

                Me.Name = o("Name").Value(Of String)
                Me.Version = o("Version").Value(Of String)
                Me.LowResolutionDataFile = o("LowResolutionDataFile").Value(Of String)
                Me.HighResolutionDataFile = o("HighResolutionDataFile").Value(Of String)

                ' Read icons
                For Each icon In JObject.FromObject(o("Icons"))

                    Dim hexValues = icon.Value.Value(Of String)
                    Dim values = New List(Of Int32)

                    For i = 0 To hexValues.Length \ 8 - 1
                        values.Add(Convert.ToInt32(hexValues.Substring(i * 8, 8), 16))
                    Next

                    Me.Icons.Add(New ResourceIcon(icon.Key, values.ToArray))
                    Dim j = Me.Icons.First.ToJson
                Next

            Else
                ' CC 2014
                Me.IsCC2014 = True

                ' Read name, filename
                Dim idx = 0
                For i = 0 To buf.Count - 1
                    If buf(i) = &HA Then
                        MarkerIndex(idx) = i
                        idx += 1
                        'If idx > 2 Then
                        '    Exit For
                        'End If
                        If idx > 4 Then
                            Exit For
                        End If

                    End If
                Next

                Me.Name = System.Text.Encoding.ASCII.GetString(buf.Take(MarkerIndex(0)).ToArray).TrimEnd(ChrW(0))
                Me.LowResolutionDataFile = System.Text.Encoding.ASCII.GetString(buf.Skip(MarkerIndex(0) + 1).Take(MarkerIndex(1) - MarkerIndex(0) - 1).ToArray).TrimEnd(ChrW(0))
                Me.HighResolutionDataFile = System.Text.Encoding.ASCII.GetString(buf.Skip(MarkerIndex(1) + 1).Take(MarkerIndex(2) - MarkerIndex(1) - 1).ToArray).TrimEnd(ChrW(0))

                ' Read icon block
                Dim offset = MarkerIndex(4) + 1 ' TODO: 2->4
                Dim structSize = NameSize + DataSize

                Dim count = (buf.Count - offset) / structSize
                Dim dst(structSize - 1) As Byte

                For i = 0 To count - 1
                    Buffer.BlockCopy(buf, Convert.ToInt32(offset + i * structSize), dst, 0, structSize)
                    Me.Icons.Add(New ResourceIcon(dst))
                Next
            End If

            Initialized = True
        End Sub

        Private Function CreateIndexFile() As Byte()

            If Not Me.IsCC2014 Then
                ' CC 
                Dim sb = New Text.StringBuilder
                sb.Append("{" & vbCrLf)
                sb.Append(vbTab & String.Format("HighResolutionDataFile: ""{0}"",", Me.HighResolutionDataFile) & vbCrLf)

                sb.Append(vbTab & "Icons: {" & vbCrLf)

                Dim icons = New List(Of String)
                For Each i In Me.Icons
                    icons.Add(vbTab & vbTab & i.ToJson)
                Next
                sb.Append(String.Join("," & vbCrLf, icons.ToArray))

                sb.Append(vbCrLf & vbTab & "}," & vbCrLf)

                sb.Append(vbTab & String.Format("LowResolutionDataFile: ""{0}"",", Me.LowResolutionDataFile) & vbCrLf)
                sb.Append(vbTab & String.Format("Name: ""{0}"",", Me.Name) & vbCrLf)
                sb.Append(vbTab & String.Format("Version: ""{0}""", Me.Version) & vbCrLf)
                sb.Append("}")

                Return Text.Encoding.ASCII.GetBytes(sb.ToString)
            Else
                ' CC 2014
                Dim buf = New List(Of Byte)

                Dim nameBuf = System.Text.Encoding.ASCII.GetBytes(Me.Name)
                buf.AddRange(nameBuf)
                For i = 0 To MarkerIndex(0) - nameBuf.Count - 1
                    buf.Add(0)
                Next
                buf.Add(&HA)

                Dim lowFileBuf = System.Text.Encoding.ASCII.GetBytes(Me.LowResolutionDataFile)
                buf.AddRange(lowFileBuf)
                For i = 0 To MarkerIndex(1) - MarkerIndex(0) - lowFileBuf.Count - 2
                    buf.Add(0)
                Next
                buf.Add(&HA)

                Dim highFileBuf = System.Text.Encoding.ASCII.GetBytes(Me.HighResolutionDataFile)
                buf.AddRange(highFileBuf)
                For i = 0 To MarkerIndex(2) - MarkerIndex(1) - highFileBuf.Count - 2
                    buf.Add(0)
                Next
                buf.Add(&HA)

                For Each icon In Me.Icons
                    buf.AddRange(icon.ToByteArray)
                Next

                Return buf.ToArray
            End If
        End Function

        Function Pack(zipStream As Stream) As Byte()

            Dim lowBuf = New List(Of Byte)
            Dim highBuf = New List(Of Byte)

            Using a = New ZipArchive(zipStream, ZipArchiveMode.Read)

                ' Initialize
                If Not Initialized Then
                    Dim indexFile = a.Entries.Where(Function(e) e.Name.ToLower.EndsWith(".idx") AndAlso e.FullName.IndexOf("/") < 0).SingleOrDefault
                    Initialize(indexFile.Open)
                    Me.IndexFileName = indexFile.Name
                End If

                ' Data
                lowBuf.AddRange(New Byte() {&H66, &H64, &H72, &H61})
                highBuf.AddRange(New Byte() {&H66, &H64, &H72, &H61})

                Dim lowOffset = lowBuf.Count
                Dim highOffset = highBuf.Count

                For Each icon In Me.Icons
                    For i = 0 To 7
                        If icon.Low.Pics(i).Size = 0 Then
                            Continue For
                        End If

                        Dim no = i
                        Dim entry = a.Entries.Where(Function(e) e.FullName = "Low/" & String.Format("{0}_s{1}.png", icon.Key, no))

                        Using ms = New MemoryStream
                            entry.Single.Open.CopyTo(ms)
                            Dim buf = ms.ToArray
                            lowBuf.AddRange(buf)
                            icon.Low.Pics(i).Offset = lowOffset
                            icon.Low.Pics(i).Size = buf.Count
                            lowOffset += buf.Count
                        End Using
                    Next

                    For i = 0 To 7
                        If icon.High.Pics(i).Size = 0 Then
                            Continue For
                        End If

                        Dim no = i
                        Dim entry = a.Entries.Where(Function(e) e.FullName = "High/" & String.Format("{0}_s{1}.png", icon.Key, no))

                        Using ms = New MemoryStream
                            entry.Single.Open.CopyTo(ms)
                            Dim buf = ms.ToArray
                            highBuf.AddRange(buf)
                            icon.High.Pics(i).Offset = highOffset
                            icon.High.Pics(i).Size = buf.Count
                            highOffset += buf.Count
                        End Using
                    Next
                Next

            End Using


            Dim s = New IO.MemoryStream

            Using a = New ZipArchive(s, ZipArchiveMode.Create)
                Using bw = New BinaryWriter(a.CreateEntry(Me.IndexFileName, CompressionLevel.Fastest).Open)
                    bw.Write(Me.CreateIndexFile.ToArray)
                End Using

                Using bw = New BinaryWriter(a.CreateEntry(Me.LowResolutionDataFile, CompressionLevel.Fastest).Open)
                    bw.Write(lowBuf.ToArray)
                End Using

                Using bw = New BinaryWriter(a.CreateEntry(Me.HighResolutionDataFile, CompressionLevel.Fastest).Open)
                    bw.Write(highBuf.ToArray)
                End Using
            End Using

            Return s.ToArray

        End Function

        Function Extract(lowResolutionDataStream As Stream, highResolutionDataStream As Stream) As Byte()

            Dim zipStream = New MemoryStream()
            Dim a = New ZipArchive(zipStream, ZipArchiveMode.Create)

            Dim lowBuf(), highBuf() As Byte

            Dim maxSize = Math.Max(
                Me.Icons.Max(Function(ico) ico.Low.Pics.Max(Function(pic) pic.Size)),
                Me.Icons.Max(Function(ico) ico.High.Pics.Max(Function(pic) pic.Size)))

            Dim dst() As Byte
            ReDim dst(maxSize - 1)

            ' Low
            Using ms = New MemoryStream
                lowResolutionDataStream.CopyTo(ms)
                lowBuf = ms.ToArray
            End Using

            For Each icon In Me.Icons
                For i = 0 To 7
                    Dim p = icon.Low.Pics(i)
                    If p.Size = 0 Then
                        Continue For
                    End If

                    Buffer.BlockCopy(lowBuf, p.Offset, dst, 0, p.Size)

                    Dim entry = a.CreateEntry("Low/" & String.Format("{0}_s{1}.png", icon.Key, i))
                    Using bw = New IO.BinaryWriter(entry.Open)
                        bw.Write(dst, 0, p.Size)
                    End Using
                Next
            Next

            ' High
            Using ms = New MemoryStream
                highResolutionDataStream.CopyTo(ms)
                highBuf = ms.ToArray
            End Using

            For Each icon In Me.Icons
                For i = 0 To 7
                    Dim p = icon.High.Pics(i)
                    If p.Size = 0 Then
                        Continue For
                    End If

                    Buffer.BlockCopy(highBuf, p.Offset, dst, 0, p.Size)

                    Dim entry = a.CreateEntry("High/" & String.Format("{0}_s{1}.png", icon.Key, i))
                    Using bw = New IO.BinaryWriter(entry.Open)
                        bw.Write(dst, 0, p.Size)
                    End Using
                Next
            Next

            ' Index (copy)
            Using bw = New IO.BinaryWriter(a.CreateEntry(Me.IndexFileName).Open)
                bw.Write(Me.CreateIndexFile.ToArray)
            End Using

            a.Dispose()

            Return zipStream.ToArray
        End Function
    End Class
End Namespace