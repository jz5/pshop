Imports System.IO
Imports System.IO.Compression

Namespace PShop.CC2017
    Public Class Archiver
        Private Sub New()
        End Sub

        Public Shared Function Pack(inZipPath As String) As Byte()
            Return Pack(File.OpenRead(inZipPath))
        End Function

        Public Shared Function Pack(inZipStream As Stream) As Byte()

            Dim lowBuf = New List(Of Byte)
            Dim highBuf = New List(Of Byte)
            Dim resources As IconResources

            Using a = New ZipArchive(inZipStream, ZipArchiveMode.Read)

                ' Get resources
                Dim indexFile = a.Entries.Where(Function(e) e.Name.ToLower.EndsWith(".idx") AndAlso e.FullName.IndexOf("/") < 0).SingleOrDefault
                resources = IconResources.FromIndexFile(indexFile.Open)
                resources.IndexFileName = indexFile.Name

                ' Data
                lowBuf.AddRange(New Byte() {&H66, &H64, &H72, &H61})
                highBuf.AddRange(New Byte() {&H66, &H64, &H72, &H61})

                Dim lowOffset = lowBuf.Count
                Dim highOffset = highBuf.Count

                For Each icon In resources.Icons
                    For i = 0 To 7
                        If icon.Low.Pics(i).Size = 0 Then
                            Continue For
                        End If

                        Dim no = i
                        Dim entry = a.Entries.Where(Function(e) e.FullName = $"Low/{icon.Key}_s{no}.png")

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
                        Dim entry = a.Entries.Where(Function(e) e.FullName = $"High/{icon.Key}_s{no}.png")

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


            Dim s = New MemoryStream

            Using a = New ZipArchive(s, ZipArchiveMode.Create)
                Using bw = New BinaryWriter(a.CreateEntry(resources.IndexFileName, CompressionLevel.Fastest).Open)
                    bw.Write(resources.ToIndexFile().ToArray)
                End Using

                Using bw = New BinaryWriter(a.CreateEntry(resources.LowResolutionDataFile, CompressionLevel.Fastest).Open)
                    bw.Write(lowBuf.ToArray)
                End Using

                Using bw = New BinaryWriter(a.CreateEntry(resources.HighResolutionDataFile, CompressionLevel.Fastest).Open)
                    bw.Write(highBuf.ToArray)
                End Using
            End Using

            Return s.ToArray

        End Function

        Public Shared Function Extract(indexFilePath As String, lowResolutionDataPath As String, highResolutionDataPath As String) As Byte()
            Return Extract(File.OpenRead(indexFilePath), File.OpenRead(lowResolutionDataPath), File.OpenRead(highResolutionDataPath))
        End Function

        Public Shared Function Extract(indexFileStream As Stream, lowResolutionDataStream As Stream, highResolutionDataStream As Stream) As Byte()
            Return Extract(IconResources.FromIndexFile(indexFileStream), lowResolutionDataStream, highResolutionDataStream)
        End Function

        Public Shared Function Extract(resources As IconResources, lowResolutionDataStream As Stream, highResolutionDataStream As Stream) As Byte()

            Dim zipStream = New MemoryStream()
            Dim a = New ZipArchive(zipStream, ZipArchiveMode.Create)

            Dim lowBuf(), highBuf() As Byte

            Dim maxSize = Math.Max(
                resources.Icons.Max(Function(ico) ico.Low.Pics.Max(Function(pic) pic.Size)),
                resources.Icons.Max(Function(ico) ico.High.Pics.Max(Function(pic) pic.Size)))

            Dim dst() As Byte
            ReDim dst(maxSize - 1)

            ' Low
            Using ms = New MemoryStream
                lowResolutionDataStream.CopyTo(ms)
                lowBuf = ms.ToArray
            End Using

            For Each icon In resources.Icons
                For i = 0 To 7
                    Dim p = icon.Low.Pics(i)
                    If p.Size = 0 Then
                        Continue For
                    End If

                    Buffer.BlockCopy(lowBuf, p.Offset, dst, 0, p.Size)

                    Dim entry = a.CreateEntry($"Low/{icon.Key}_s{i}.png")
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

            For Each icon In resources.Icons
                For i = 0 To 7
                    Dim p = icon.High.Pics(i)
                    If p.Size = 0 Then
                        Continue For
                    End If

                    Buffer.BlockCopy(highBuf, p.Offset, dst, 0, p.Size)

                    Dim entry = a.CreateEntry($"High/{icon.Key}_s{i}.png")
                    Using bw = New BinaryWriter(entry.Open)
                        bw.Write(dst, 0, p.Size)
                    End Using
                Next
            Next

            ' Index (copy)
            Using bw = New BinaryWriter(a.CreateEntry(resources.IndexFileName).Open)
                bw.Write(resources.ToIndexFile().ToArray)
            End Using

            a.Dispose()

            Return zipStream.ToArray
        End Function
    End Class
End Namespace
