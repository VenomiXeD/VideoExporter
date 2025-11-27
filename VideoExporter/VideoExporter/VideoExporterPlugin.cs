using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using Frosty.Core;
using Frosty.Core.Attributes;
using FrostySdk.Interfaces;
using VideoViewer;

[assembly: PluginAuthor("MeanPartyRose, SprettWasHere")]
[assembly: PluginDisplayName("Video Exporter")]
[assembly: PluginVersion("1.0.1.0")]
[assembly: RegisterStartupAction(typeof(VideoExporterPlugin))]

namespace VideoViewer;

public class VideoExporterPlugin : StartupAction
{
    private const string VGMSTREAM_DOWNLOAD_URL =
        "https://github.com/vgmstream/vgmstream-releases/releases/download/nightly/vgmstream-win64.zip";

    private const string FFMPEG_DOWNLOAD_URL = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-git-essentials.7z";

    private const string SEVENZIP_CLI_URL = "https://www.7-zip.org/a/7zr.exe";
    private static string UTILS_FOLDER => Path.GetFullPath("./videoexporter_utils");

    private static string FFMPEG_FOLDER => Path.Combine(UTILS_FOLDER, "ffmpeg");
    private static string VGMSTREAM_FOLDER => Path.Combine(UTILS_FOLDER, "vgmstream");

    public static string FFMPEG_EXECUTABLE => !Directory.Exists(FFMPEG_FOLDER)
        ? null
        : Path.Combine(Directory.GetDirectories(FFMPEG_FOLDER).FirstOrDefault(), "bin", "ffmpeg.exe");

    public static string VGMSTREAM_EXECUTABLE => !Directory.Exists(VGMSTREAM_FOLDER)
        ? null
        : Path.Combine(VGMSTREAM_FOLDER, "vgmstream-cli.exe");

    public override Action<ILogger> Action => OnStartup;

    private async void OnStartup(ILogger obj)
    {
        Directory.CreateDirectory(UTILS_FOLDER);

        using (WebClient wc = new())
        {
            wc.DownloadProgressChanged += (sender, args) =>
                App.Logger.Log($"Downloading progress: {args.ProgressPercentage}%");

            if (!Directory.Exists(Path.Combine(UTILS_FOLDER, "ffmpeg")))
            {
                DialogResult dialogResult = MessageBox.Show("FFmpeg is not installed\nDo you wish to set it up?",
                    "Missing ffmpeg",
                    MessageBoxButtons.YesNo);

                if (dialogResult == DialogResult.Yes)
                {
                    string ffmpeg7z = Path.Combine(UTILS_FOLDER, "ffmpeg.7z");

                    await wc.DownloadFileTaskAsync(FFMPEG_DOWNLOAD_URL, ffmpeg7z);
                    await wc.DownloadFileTaskAsync(SEVENZIP_CLI_URL, Path.Combine(UTILS_FOLDER, "7zr.exe"));

                    ProcessStartInfo info = new(Path.Combine(UTILS_FOLDER, "7zr.exe"));
                    info.UseShellExecute = false;
                    info.Arguments = $"x \"{ffmpeg7z}\" -o\"{FFMPEG_FOLDER}\" -y";

                    Process.Start(info).WaitForExit();
                }
            }

            if (!Directory.Exists(VGMSTREAM_FOLDER))
            {
                DialogResult dialogResult = MessageBox.Show("VGMStream is not installed\nDo you wish to set it up?",
                    "Missing VGMStream",
                    MessageBoxButtons.YesNo);

                if (dialogResult == DialogResult.Yes)
                {
                    string vgmstreamZip = Path.Combine(UTILS_FOLDER, "vgmstream.zip");
                    await wc.DownloadFileTaskAsync(VGMSTREAM_DOWNLOAD_URL, vgmstreamZip);

                    ZipFile.ExtractToDirectory(vgmstreamZip, VGMSTREAM_FOLDER);
                }
            }
            
            App.Logger.Log(string.IsNullOrWhiteSpace(FFMPEG_EXECUTABLE) ? "FFmpeg: Missing " : "FFmpeg: Ready");
            App.Logger.Log(string.IsNullOrWhiteSpace(VGMSTREAM_EXECUTABLE) ? "VGMStream: Missing " : "VGMStream: Ready");
        }
    }
}