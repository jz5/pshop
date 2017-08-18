Imports System.IO
Imports System.Text

Namespace PShop.CC2017
    Public Class IconResources
        Private Const NameSize As Integer = 48
        Private Const DataSize As Integer = 320

        Property IndexFileName As String
        Property Name As String
        Property Version As String
        Property LowResolutionDataFile As String
        Property HighResolutionDataFile As String
        Property XLowResolutionDataFile As String
        Property XHighResolutionDataFile As String
        Property Icons As New List(Of ResourceIcon)
        Property MarkerIndexes As New List(Of Integer)

        Public Shared Function FromIndexFile(indexFilePath As String) As IconResources
            Return FromIndexFile(File.OpenRead(indexFilePath), Path.GetFileName(indexFilePath))
        End Function

        Public Shared Function FromIndexFile(indexFileStream As Stream, Optional indexFilename As String = "IconResources.idx") As IconResources

            Dim resources = New IconResources With {
                .IndexFileName = indexFilename
            }

            Dim buf() As Byte
            Using ms = New MemoryStream
                indexFileStream.CopyTo(ms)
                buf = ms.ToArray
            End Using

            ' Read name, filename
            Dim idx = 0
            For i = 0 To buf.Count - 1
                If buf(i) = &HA Then
                    resources.MarkerIndexes.Add(i)
                    idx += 1

                    If idx > 4 Then
                        Exit For
                    End If
                End If
            Next

            resources.Name = Encoding.ASCII.GetString(buf.Take(resources.MarkerIndexes(0)).ToArray).TrimEnd(ChrW(0))
            resources.LowResolutionDataFile = Encoding.ASCII.GetString(buf.Skip(resources.MarkerIndexes(0) + 1).Take(resources.MarkerIndexes(1) - resources.MarkerIndexes(0) - 1).ToArray).TrimEnd(ChrW(0))
            resources.HighResolutionDataFile = Encoding.ASCII.GetString(buf.Skip(resources.MarkerIndexes(1) + 1).Take(resources.MarkerIndexes(2) - resources.MarkerIndexes(1) - 1).ToArray).TrimEnd(ChrW(0))
            resources.XLowResolutionDataFile = Encoding.ASCII.GetString(buf.Skip(resources.MarkerIndexes(2) + 1).Take(resources.MarkerIndexes(3) - resources.MarkerIndexes(2) - 1).ToArray).TrimEnd(ChrW(0))
            resources.XHighResolutionDataFile = Encoding.ASCII.GetString(buf.Skip(resources.MarkerIndexes(3) + 1).Take(resources.MarkerIndexes(4) - resources.MarkerIndexes(3) - 1).ToArray).TrimEnd(ChrW(0))

            ' Read icon block
            Dim offset = resources.MarkerIndexes(4) + 1
            Dim structSize = NameSize + DataSize

            Dim count = (buf.Count - offset) / structSize
            Dim dst(structSize - 1) As Byte

            For i = 0 To count - 1
                Buffer.BlockCopy(buf, Convert.ToInt32(offset + i * structSize), dst, 0, structSize)
                resources.Icons.Add(New ResourceIcon(dst))
            Next

            Return resources
        End Function

        Public Function ToIndexFile() As Byte()
            Dim buf = New List(Of Byte)

            Dim nameBuf = Encoding.ASCII.GetBytes(Name)
            buf.AddRange(nameBuf)
            For i = 0 To MarkerIndexes(0) - nameBuf.Count - 1
                buf.Add(0)
            Next
            buf.Add(&HA)

            Dim lowFileBuf = Encoding.ASCII.GetBytes(LowResolutionDataFile)
            buf.AddRange(lowFileBuf)
            For i = 0 To MarkerIndexes(1) - MarkerIndexes(0) - lowFileBuf.Count - 2
                buf.Add(0)
            Next
            buf.Add(&HA)

            Dim highFileBuf = Encoding.ASCII.GetBytes(HighResolutionDataFile)
            buf.AddRange(highFileBuf)
            For i = 0 To MarkerIndexes(2) - MarkerIndexes(1) - highFileBuf.Count - 2
                buf.Add(0)
            Next
            buf.Add(&HA)

            Dim xLowFileBuf = Encoding.ASCII.GetBytes(XLowResolutionDataFile)
            buf.AddRange(xLowFileBuf)
            For i = 0 To MarkerIndexes(3) - MarkerIndexes(2) - xLowFileBuf.Count - 2
                buf.Add(0)
            Next
            buf.Add(&HA)

            Dim xHighFileBuf = Encoding.ASCII.GetBytes(XHighResolutionDataFile)
            buf.AddRange(xHighFileBuf)
            For i = 0 To MarkerIndexes(4) - MarkerIndexes(3) - xHighFileBuf.Count - 2
                buf.Add(0)
            Next
            buf.Add(&HA)

            For Each icon In Icons
                buf.AddRange(icon.ToByteArray)
            Next

            Return buf.ToArray
        End Function

    End Class
End Namespace