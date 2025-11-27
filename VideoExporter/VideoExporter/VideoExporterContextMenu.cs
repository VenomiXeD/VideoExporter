using System.Diagnostics;
using System.Reflection;
using Frosty.Core;
using Frosty.Core.Attributes;
using VideoViewer;

// [assembly: RegisterStartupAction(typeof(BestPlugin))]

[assembly: RegisterDataExplorerContextMenu(typeof(VideoExporterContextMenu))]

namespace VideoViewer;

public class VideoExporterContextMenu : DataExplorerContextMenuExtension
{
    public override string ContextItemName => "Export Video";
    public override RelayCommand ContextItemClicked => new(OnContextMenuClicked);

    private async void OnContextMenuClicked(object obj)
    {
        if (string.IsNullOrWhiteSpace(VideoExporterPlugin.FFMPEG_EXECUTABLE) ||
            string.IsNullOrWhiteSpace(VideoExporterPlugin.VGMSTREAM_EXECUTABLE))
        {
            App.Logger.LogWarning("VideoExporter plugin could not find the FFmpeg/VGMStream-CLI executable. Restarting Frosty will prompt to install these two utilities.");
            return;
        }
        object ebxObject = App.AssetManager.GetEbx(App.SelectedAsset)?.RootObject;
        PropertyInfo chunkGuidProperty = ebxObject?.GetType().GetProperty("ChunkGuid");
        if (chunkGuidProperty is null)
        {
            App.Logger.LogWarning("Selected Asset has no ChunkGuid to refer to.");
            return;
        }

        Guid chunkGuid = Guid.Parse(chunkGuidProperty.GetValue(ebxObject).ToString());


        Directory.CreateDirectory("exported");
        using FileStream fs = File.Create($"./exported/{chunkGuid.ToString()}.vp6");
        using Stream chunkStream = App.AssetManager.GetChunk(App.AssetManager.GetChunkEntry(chunkGuid));
        chunkStream.CopyTo(fs);

        fs.Close();
        fs.Dispose();

        string inputFile = Path.Combine(AppContext.BaseDirectory, "exported", $"{chunkGuid.ToString()}.vp6");
        string outputFileBaseMp4 =
            Path.Combine(AppContext.BaseDirectory, "exported", $"{App.SelectedAsset.DisplayName}-vid.mp4");
        string outputFileFullMp4 = Path.Combine(AppContext.BaseDirectory, "exported",
            $"{App.SelectedAsset.DisplayName}-merged.mp4");
        string outputFileAudioWav =
            Path.Combine(AppContext.BaseDirectory, "exported", $"{App.SelectedAsset.DisplayName}.wav");

        // Convert it to a mp4 first
        ProcessStartInfo processStart;
        processStart = new ProcessStartInfo(VideoExporterPlugin.FFMPEG_EXECUTABLE)
            { UseShellExecute = false };

        processStart.Arguments =
            $"-i \"{inputFile}\" -c:v libx264 -crf 18 -preset slow -c:a aac -b:a 192k \"{outputFileBaseMp4}\"";
        App.Logger.Log($"[EXEC] {processStart.FileName} {processStart.Arguments}");
        await Task.Delay(100);
        Process.Start(processStart).WaitForExit();


        // Get the wav output of the chunk
        processStart = new ProcessStartInfo(VideoExporterPlugin.VGMSTREAM_EXECUTABLE)
            { UseShellExecute = false };
        processStart.Arguments = $"-o \"{outputFileAudioWav}\" \"{inputFile}\"";

        App.Logger.Log($"[EXEC] {processStart.FileName} {processStart.Arguments}");
        await Task.Delay(100);
        Process.Start(processStart).WaitForExit();

        // Merge wav and video
        // ffmpeg -i input_video.mp4 -i input_audio.wav -c:v copy -c:a aac -map 0:v:0 -map 1:a:0 output.mp4
        processStart = new ProcessStartInfo(VideoExporterPlugin.FFMPEG_EXECUTABLE)
            { UseShellExecute = false };
        processStart.Arguments =
            $"-i \"{outputFileBaseMp4}\" -i \"{outputFileAudioWav}\" -c:v copy -c:a aac -map 0:v:0 -map 1:a:0 \"{outputFileFullMp4}\"";

        App.Logger.Log($"[EXEC] {processStart.FileName} {processStart.Arguments}");
        await Task.Delay(100);
        Process.Start(processStart).WaitForExit();
        
        App.Logger.Log($"Video export success: {outputFileFullMp4}");
    }
}