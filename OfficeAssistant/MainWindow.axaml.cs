using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;


namespace OfficeAssistant;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<string> selectedFiles;
    private string? lastSavedFile;

    public MainWindow()
    {
        InitializeComponent();
        selectedFiles = [];
        DataContext = this;
    }

    public ObservableCollection<string> SelectedFiles => selectedFiles;

    private async Task SelectFiles()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择PDF文件",
            AllowMultiple = true,
            FileTypeFilter = [FilePickerFileTypes.Pdf]
        });

        foreach (var file in files)
        {
            if (!selectedFiles.Contains(file.Path.LocalPath))
            {
                selectedFiles.Add(file.Path.LocalPath);
            }
        }
    }

    private async Task MergeFiles()
    {
        if (selectedFiles.Count < 2) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存合并后的PDF",
            DefaultExtension = "pdf",
            FileTypeChoices = [FilePickerFileTypes.Pdf]
        });

        if (file == null) return;

        try
        {
            using (var output = new PdfDocument())
            {
                foreach (var path in selectedFiles)
                {
                    using var input = PdfReader.Open(path, PdfDocumentOpenMode.Import);
                    for (var i = 0; i < input.PageCount; i++)
                    {
                        output.AddPage(input.Pages[i]);
                    }
                }
                output.Save(file.Path.LocalPath);
            }

            lastSavedFile = file.Path.LocalPath;

            // 显示成功消息
            var messageText = this.FindControl<TextBlock>("MessageText");
            if (messageText != null)
            {
                messageText.Text = $"PDF文件已成功合并并保存到：{file.Path.LocalPath}";
                messageText.Opacity = 1;

                // 3秒后淡出消息
                await Task.Delay(3000);
                messageText.Opacity = 0;
            }
        }
        catch (Exception ex)
        {
            var messageText = this.FindControl<TextBlock>("MessageText");
            if (messageText != null)
            {
                messageText.Text = $"合并PDF文件时发生错误：{ex.Message}";
                messageText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Red);
                messageText.Opacity = 1;

                // 3秒后淡出消息
                await Task.Delay(3000);
                messageText.Opacity = 0;
                messageText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Green);
            }
        }
    }

    private void RemoveFile(string file)
    {
        selectedFiles.Remove(file);
    }

    private async void SelectFiles(object sender, RoutedEventArgs e)
    {
        await SelectFiles();
    }

    private async void MergeFiles(object sender, RoutedEventArgs e)
    {
        await MergeFiles();
    }

    private void RemoveFile(object sender, RoutedEventArgs e)
    {
        if (e.Source is Button button && button.CommandParameter is string file)
        {
            RemoveFile(file);
        }
    }

    private void OnNavigationSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavigationList.SelectedIndex == 0)
        {
            PdfMergeView.IsVisible = true;
            PdfSplitView.IsVisible = false;
            PdfReplaceView.IsVisible = false;
        }
        else if (NavigationList.SelectedIndex == 1)
        {
            PdfMergeView.IsVisible = false;
            PdfSplitView.IsVisible = true;
            PdfReplaceView.IsVisible = false;
        }
        else if (NavigationList.SelectedIndex == 2)
        {
            PdfMergeView.IsVisible = false;
            PdfSplitView.IsVisible = false;
            PdfReplaceView.IsVisible = true;
        }
    }
}