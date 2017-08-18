Namespace PShop.CC
    <Obsolete>
    Public Class ResourceIcon
        Private Const NameSize As Integer = 48
        Private Const DataSize As Integer = 160

        Property Key As String
        Property Low As IconData
        Property High As IconData

        ' For CC
        Sub New(key As String, values() As Int32)
            Me.Key = key

            Me.Low = New IconData With {
                        .Width = values(0),
                        .Height = values(1),
                        .X = values(2),
                        .Y = values(3)}

            Me.High = New IconData With {
                        .Width = values(20),
                        .Height = values(21),
                        .X = values(22),
                        .Y = values(23)}

            For i = 0 To 7
                Me.Low.Pics.Add(New PicInfo With {
                                     .Offset = values(4 + i * 2),
                                     .Size = values(4 + i * 2 + 1)})
            Next

            For i = 0 To 7
                Me.High.Pics.Add(New PicInfo With {
                                      .Offset = values(24 + i * 2),
                                      .Size = values(24 + i * 2 + 1)})
            Next
        End Sub

        ' For CC 2014
        Sub New(buffer() As Byte)

            Me.Key = System.Text.Encoding.ASCII.GetString(buffer.Take(NameSize).ToArray).TrimEnd(ChrW(0))

            Dim data = New List(Of Int32)
            For j = 0 To DataSize / 4 - 1
                data.Add(BitConverter.ToInt32(buffer, Convert.ToInt32(NameSize + j * 4)))
            Next

            Me.Low = New IconData With {
                .Width = data(0),
                .Height = data(2),
                .X = data(4),
                .Y = data(6)
            }
            For i = 0 To 8 - 1
                Me.Low.Pics.Add(New PicInfo With {
                            .Offset = data(8 + i),
                            .Size = data(24 + i)})
            Next

            Me.High = New IconData With {
                .Width = data(1),
                .Height = data(3),
                .X = data(5),
                .Y = data(7)
            }
            For i = 0 To 8 - 1
                Me.High.Pics.Add(New PicInfo With {
                            .Offset = data(16 + i),
                            .Size = data(32 + i)})
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
            buf.AddRange(BitConverter.GetBytes(Me.Low.Height))
            buf.AddRange(BitConverter.GetBytes(Me.High.Height))
            buf.AddRange(BitConverter.GetBytes(Me.Low.X))
            buf.AddRange(BitConverter.GetBytes(Me.High.X))
            buf.AddRange(BitConverter.GetBytes(Me.Low.Y))
            buf.AddRange(BitConverter.GetBytes(Me.High.Y))
            For i = 0 To 8 - 1
                buf.AddRange(BitConverter.GetBytes(Me.Low.Pics(i).Offset))
            Next
            For i = 0 To 8 - 1
                buf.AddRange(BitConverter.GetBytes(Me.High.Pics(i).Offset))
            Next
            For i = 0 To 8 - 1
                buf.AddRange(BitConverter.GetBytes(Me.Low.Pics(i).Size))
            Next
            For i = 0 To 8 - 1
                buf.AddRange(BitConverter.GetBytes(Me.High.Pics(i).Size))
            Next
            Return buf.ToArray
        End Function


        Function ToJson() As String

            Dim sb = New Text.StringBuilder
            sb.Append(Me.Low.Width.ToString("X8"))
            sb.Append(Me.Low.Height.ToString("X8"))
            sb.Append(Me.Low.X.ToString("X8"))
            sb.Append(Me.Low.Y.ToString("X8"))

            For i = 0 To 7
                Dim p = Me.Low.Pics(i)
                sb.Append(p.Offset.ToString("X8"))
                sb.Append(p.Size.ToString("X8"))
            Next

            sb.Append(Me.High.Width.ToString("X8"))
            sb.Append(Me.High.Height.ToString("X8"))
            sb.Append(Me.High.X.ToString("X8"))
            sb.Append(Me.High.Y.ToString("X8"))

            For i = 0 To 7
                Dim p = Me.High.Pics(i)
                sb.Append(p.Offset.ToString("X8"))
                sb.Append(p.Size.ToString("X8"))
            Next

            Return String.Format("{0}: ""{1}""", Me.Key, sb.ToString)
        End Function

    End Class
End Namespace