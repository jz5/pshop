Namespace PShop.CC2017
    Public Class ResourceIcon
        Private Const NameSize As Integer = 48
        Private Const DataSize As Integer = 320

        Property Key As String
        Property Low As IconData
        Property High As IconData

        ' For CC 2015
        Sub New(buffer() As Byte)

            Me.Key = System.Text.Encoding.ASCII.GetString(buffer.Take(NameSize).ToArray).TrimEnd(ChrW(0))

            Dim data = New List(Of Int32)
            For j = 0 To DataSize / 4 - 1
                data.Add(BitConverter.ToInt32(buffer, Convert.ToInt32(NameSize + j * 4)))
            Next

            Me.Low = New IconData With {
                .Width = data(0),
                .Height = data(4),
                .X = data(8),
                .Y = data(12)
            }
            For i = 0 To 8 - 1
                Me.Low.Pics.Add(New PicInfo With {
                            .Offset = data(16 + i),
                            .Size = data(48 + i)})
            Next

            Me.High = New IconData With {
                .Width = data(1),
                .Height = data(5),
                .X = data(9),
                .Y = data(13)
            }
            For i = 0 To 8 - 1
                Me.High.Pics.Add(New PicInfo With {
                            .Offset = data(24 + i),
                            .Size = data(56 + i)})
            Next

        End Sub

        Function ToByteArray() As Byte()
            Dim buf = New List(Of Byte)

            ' Key
            Dim key = System.Text.Encoding.ASCII.GetBytes(Me.Key)
            buf.AddRange(key)
            For i = 0 To NameSize - key.Count - 1
                buf.Add(0)
            Next

            ' Data
            buf.AddRange(BitConverter.GetBytes(Me.Low.Width))
            buf.AddRange(BitConverter.GetBytes(Me.High.Width))
            buf.AddRange(BitConverter.GetBytes(0))
            buf.AddRange(BitConverter.GetBytes(0))
            buf.AddRange(BitConverter.GetBytes(Me.Low.Height))
            buf.AddRange(BitConverter.GetBytes(Me.High.Height))
            buf.AddRange(BitConverter.GetBytes(0))
            buf.AddRange(BitConverter.GetBytes(0))
            buf.AddRange(BitConverter.GetBytes(Me.Low.X))
            buf.AddRange(BitConverter.GetBytes(Me.High.X))
            buf.AddRange(BitConverter.GetBytes(0))
            buf.AddRange(BitConverter.GetBytes(0))
            buf.AddRange(BitConverter.GetBytes(Me.Low.Y))
            buf.AddRange(BitConverter.GetBytes(Me.High.Y))
            buf.AddRange(BitConverter.GetBytes(0))
            buf.AddRange(BitConverter.GetBytes(0))
            For i = 0 To 8 - 1
                buf.AddRange(BitConverter.GetBytes(Me.Low.Pics(i).Offset))
            Next
            For i = 0 To 8 - 1
                buf.AddRange(BitConverter.GetBytes(Me.High.Pics(i).Offset))
            Next
            For i = 0 To 8 - 1
                buf.AddRange(BitConverter.GetBytes(0))
            Next
            For i = 0 To 8 - 1
                buf.AddRange(BitConverter.GetBytes(0))
            Next
            For i = 0 To 8 - 1
                buf.AddRange(BitConverter.GetBytes(Me.Low.Pics(i).Size))
            Next
            For i = 0 To 8 - 1
                buf.AddRange(BitConverter.GetBytes(Me.High.Pics(i).Size))
            Next
            For i = 0 To 8 - 1
                buf.AddRange(BitConverter.GetBytes(0))
            Next
            For i = 0 To 8 - 1
                buf.AddRange(BitConverter.GetBytes(0))
            Next
            Return buf.ToArray
        End Function

    End Class
End Namespace
