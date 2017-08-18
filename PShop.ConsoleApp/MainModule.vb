Imports System.IO
Imports PShop.PShop.CC2017

Module MainModule

    ''' <summary>
    ''' Filer Sample
    ''' </summary>
    ''' <param name="args"></param>
    Sub Main(args As String())
        Dim inputPath As String
        Dim outputPath As String
        Dim packing = False

        If args.Length >= 2 AndAlso (args(0) = "-e" OrElse args(0) = "-p") Then
            packing = args(0) = "-p"

            inputPath = args(1)
            outputPath = If(args.Length >= 3, args(2), Path.Combine(My.Application.Info.DirectoryPath, "Work"))
        Else
            ShowUsage()
            Exit Sub
        End If

        If packing Then
            ' Pack
            Console.WriteLine("Packing icons...")
            Filer.Pack(inputPath, outputPath)
        Else
            ' Extract
            Console.WriteLine("Extracting icons...")
            Filer.Extract(inputPath, outputPath)
        End If

    End Sub

    Private Sub ShowUsage()
        Console.WriteLine("Usage:")
        Console.WriteLine("  Extract icons: psccIcon -e ""C:\Program Files\Adobe\Adobe Photoshop CC 2017\Resources"" ""output folder path""")
        Console.WriteLine("  Pack icons:    psccIcon -p ""input folder path"" ""output folder path""")
    End Sub

    ''' <summary>
    ''' Archiver Sample
    ''' </summary>
    ''' <param name="args"></param>
    Sub Zip(args As String())

        ' Usage
        ' psccIcon -p extracting.zip packing.zip
        ' psccIcon -e IconResources.idx PSIconsLowRes.dat PSIconsHighRes.dat extracting.zip

        Dim packing = args(0) = "-p"
        If packing Then
            ' Pack
            Console.WriteLine("Packing icons...")
            Dim zip = Archiver.Pack(args(1))
            File.WriteAllBytes(args(2), zip)
        Else
            ' Extract
            Console.WriteLine("Extracting icons...")
            Dim zip = Archiver.Extract(args(1), args(2), args(3))
            File.WriteAllBytes(args(4), zip)
        End If
    End Sub

End Module
