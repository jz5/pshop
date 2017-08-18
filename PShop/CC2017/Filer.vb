Imports System.IO

Namespace PShop.CC2017
    Public Class Filer
        Private Sub New()
        End Sub

        Public Shared Sub Pack(inputPath As String, outputPath As String, Optional indexFilename As String = "IconResources.idx")

            Directory.CreateDirectory(Path.Combine(outputPath))

            Dim indexFilePath = Path.Combine(inputPath, indexFilename)
            Dim resources = IconResources.FromIndexFile(indexFilePath)

            Dim lowBuf = New List(Of Byte)
            Dim highBuf = New List(Of Byte)

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
                    Dim png = Path.Combine(inputPath, "Low", $"{icon.Key}_s{no}.png")

                    Using ms = New MemoryStream
                        File.OpenRead(png).CopyTo(ms)
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
                    Dim png = Path.Combine(inputPath, "High", $"{icon.Key}_s{no}.png")

                    Using ms = New MemoryStream
                        File.OpenRead(png).CopyTo(ms)
                        Dim buf = ms.ToArray
                        highBuf.AddRange(buf)
                        icon.High.Pics(i).Offset = highOffset
                        icon.High.Pics(i).Size = buf.Count
                        highOffset += buf.Count
                    End Using
                Next
            Next

            Using bw = New BinaryWriter(File.OpenWrite(Path.Combine(outputPath, resources.IndexFileName)))
                bw.Write(resources.ToIndexFile().ToArray)
            End Using

            Using bw = New BinaryWriter(File.OpenWrite(Path.Combine(outputPath, resources.LowResolutionDataFile)))
                bw.Write(lowBuf.ToArray)
            End Using

            Using bw = New BinaryWriter(File.OpenWrite(Path.Combine(outputPath, resources.HighResolutionDataFile)))
                bw.Write(highBuf.ToArray)
            End Using

        End Sub

        Public Shared Sub Extract(resourcesPath As String, outputPath As String, Optional indexFilename As String = "IconResources.idx")

            Directory.CreateDirectory(Path.Combine(outputPath))
            Directory.CreateDirectory(Path.Combine(outputPath, "Low"))
            Directory.CreateDirectory(Path.Combine(outputPath, "High"))

            Dim indexFilePath = Path.Combine(resourcesPath, indexFilename)
            Dim resources = IconResources.FromIndexFile(indexFilePath)

            Dim lowBuf(), highBuf() As Byte

            Dim maxSize = Math.Max(
                resources.Icons.Max(Function(ico) ico.Low.Pics.Max(Function(pic) pic.Size)),
                resources.Icons.Max(Function(ico) ico.High.Pics.Max(Function(pic) pic.Size)))

            Dim dst() As Byte
            ReDim dst(maxSize - 1)

            ' Low
            Using ms = New MemoryStream
                File.OpenRead(Path.Combine(resourcesPath, resources.LowResolutionDataFile)).CopyTo(ms)
                lowBuf = ms.ToArray
            End Using

            For Each icon In resources.Icons
                For i = 0 To 7
                    Dim p = icon.Low.Pics(i)
                    If p.Size = 0 Then
                        Continue For
                    End If

                    Buffer.BlockCopy(lowBuf, p.Offset, dst, 0, p.Size)

                    Dim stream = File.OpenWrite(Path.Combine(outputPath, "Low", $"{icon.Key}_s{i}.png"))
                    Using bw = New BinaryWriter(stream)
                        bw.Write(dst, 0, p.Size)
                    End Using
                Next
            Next

            ' High
            Using ms = New MemoryStream
                File.OpenRead(Path.Combine(resourcesPath, resources.HighResolutionDataFile)).CopyTo(ms)
                highBuf = ms.ToArray
            End Using

            For Each icon In resources.Icons
                For i = 0 To 7
                    Dim p = icon.High.Pics(i)
                    If p.Size = 0 Then
                        Continue For
                    End If

                    Buffer.BlockCopy(highBuf, p.Offset, dst, 0, p.Size)

                    Dim stream = File.OpenWrite(Path.Combine(outputPath, "High", $"{icon.Key}_s{i}.png"))
                    Using bw = New BinaryWriter(stream)
                        bw.Write(dst, 0, p.Size)
                    End Using
                Next
            Next

            ' Index (copy)
            File.Copy(indexFilePath, Path.Combine(outputPath, indexFilename))

        End Sub
    End Class
End Namespace
