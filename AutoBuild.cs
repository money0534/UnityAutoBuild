using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
// this uses the Unity port of DotNetZip https://github.com/r2d2rigo/dotnetzip-for-unity
using Ionic.Zip;

// place in an "Editor" folder in your Assets folder
public class AutoBuild
{
    // TODO: turn this into a wizard or something??? whatever

    /// <summary>
    /// 时间戳格式 用于版本号及压缩包
    /// </summary>
    public static string TIME_FORMAT = "yyyyMMdd";
//    public static string TIME_FORMAT = "yyyyMMdd HH:mm:ss";
//    public static string ZIP_TIME_FORMAT = "yyyyMMdd HH_mm_ss";
    private static string bundleVersion;


    //MSDN: https://docs.microsoft.com/en-us/dotnet/api/system.io.compression.ziparchive?redirectedfrom=MSDN&view=netframework-4.7.2
//	static void Main(string[] args)
//	{
//		using (FileStream zipToOpen = new FileStream(@"c:\users\exampleuser\release.zip", FileMode.Open))
//		{
//			using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
//			{
//				ZipArchiveEntry readmeEntry = archive.CreateEntry("Readme.txt");
//				using (StreamWriter writer = new StreamWriter(readmeEntry.Open()))
//				{
//					writer.WriteLine("Information about this package.");
//					writer.WriteLine("========================");
//				}
//			}
//		}
//	}

    [MenuItem("打包/Build Windows64")]
    public static void StartWindows()
    {
        // Get filename.
//		string path = EditorUtility.SaveFolderPanel("Build out WINDOWS to...",
//		                                            GetProjectFolderPath() + "/Builds/",
//		                                            "");
//		var filename = path.Split('/'); // do this so I can grab the project folder name
//        var buildPath = "D:\\Unity\\Projects\\Builds";
        var buildPath = GetProjectFolderPath() + "/Builds/";

        var name = BuildConfig.SchoolName;

        var appDir = buildPath + name;

        Debug.Log("Start building at "+appDir);


        PreBuild(buildPath, appDir);
		BuildPlayer ( BuildTarget.StandaloneWindows64, name, buildPath );//BuildTarget.StandaloneWindows
    }

    static void DeleteAll(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        //删除所有文件
        foreach (var file in Directory.GetFiles(path))
        {
//            //保留版本信息
//            if (file.Contains("version.ini"))
//            {
//                continue;
//            }

            File.Delete(file);
        }

        //递归删除所有文件夹
        foreach (var subDir in Directory.GetDirectories(path))
        {
//            //保留msc文件夹
//            if (subDir.Contains("msc"))
//            {
//                continue;
//            }

            DeleteAll(subDir);
            Directory.Delete(subDir);
        }
    }

    /// <summary>
    /// 删除之前的内容
    /// </summary>
    /// <param name="s"></param>
    private static void PreBuild(string buildDir, string appDir)
    {
        if (!Directory.Exists(buildDir))
        {
            Directory.CreateDirectory(buildDir);
        }
        if (!Directory.Exists(appDir))
        {
            Directory.CreateDirectory(appDir);
        }

//        Debug.Log("【AutoBuild】"+buildDir);
//        Debug.Log("【AutoBuild】"+appDir);

        DeleteAll(appDir);

//        WriteVersion(appDir);
        IncreaseVersion();

        CopyMsc(appDir);
    }

    private static void IncreaseVersion()
    {
        var oldVersion = PlayerSettings.bundleVersion;
        Debug.Log("【AutoBuild】当前版本："+oldVersion);
        float oldVersionCode = float.Parse(oldVersion.Split('_')[0].Replace("v",""));
        float newVersionCode = oldVersionCode + 0.1f;
        bundleVersion = string.Format("v{0}_{1}", newVersionCode, DateTime.Now.ToString(TIME_FORMAT));
        PlayerSettings.bundleVersion = bundleVersion;
        Debug.Log("【AutoBuild】最新版本："+bundleVersion);
    }

    /// <summary>
    /// 拷贝msc资源文件 讯飞语音
    /// </summary>
    /// <param name="appDir"></param>
    private static void CopyMsc(string appDir)
    {
        string sourceDirectory = GetProjectFolderPath()+"/msc";
        string targetDirectory = appDir+"/msc";

        DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
        DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

        CopyAll(diSource, diTarget);
    }

    public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
    {
        if (source.FullName.ToLower() == target.FullName.ToLower())
        {
            return;
        }

        // Check if the target directory exists, if not, create it.
        if (Directory.Exists(target.FullName) == false)
        {
            Directory.CreateDirectory(target.FullName);
        }

        // Copy each file into it's new directory.
        foreach (FileInfo fi in source.GetFiles())
        {
//            Debug.Log(string.Format(@"Copying {0}\{1}", target.FullName, fi.Name));
            fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir =
                target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }

    /// <summary>
    /// 自动写入版本信息
    /// </summary>
    /// <param name="dstPath"></param>
    private static void WriteVersion(string dstPath)
    {
        dstPath += "/version.ini";
        var srcPath = Application.dataPath + "/Plugins/version.ini";
//        Debug.Log("【BuildRadiator】" + srcPath);
//        Debug.Log("【BuildRadiator】" + dstPath);
//        Debug.Log("【AutoBuild】" + File.Exists(srcPath) + "," + File.Exists(dstPath));


        //1.读旧版本信息
        string oldVersion;
        using (StreamReader sr = new StreamReader(srcPath))
        {
            oldVersion = sr.ReadToEnd();
        }

        //2.更新版本号，写时间戳
        string newVersion;
        using (StreamWriter sw = new StreamWriter(srcPath, false))
        {
            int oldVersionCode = int.Parse(oldVersion.Split(',')[0]);
            var newVersionCode = oldVersionCode + 1;
            newVersion =
                newVersionCode + ", BuildTime: " + DateTime.Now.ToString(TIME_FORMAT);
            sw.WriteLine(newVersion);
        }

        //3.同步到打包路径
//        if (!File.Exists(dstPath))
//        {
//            File.CreateText(dstPath);
//        }
        using (StreamWriter sw = new StreamWriter(dstPath, false))
        {
            sw.WriteLine(newVersion);
        }
    }


//	[MenuItem("BuildRadiator/Build Windows + Mac OSX + Linux")]
    public static void StartAll()
    {
        // Get filename.
        string path = EditorUtility.SaveFolderPanel("Build out ALL STANDALONES to...",
            GetProjectFolderPath() + "/Builds/",
            "");
        var filename = path.Split('/'); // do this so I can grab the project folder name
//		BuildPlayer ( BuildTarget.StandaloneOSXUniversal, filename[filename.Length-1], path + "/" );
        BuildPlayer(BuildTarget.StandaloneLinuxUniversal, filename[filename.Length - 1],
            path + "/");
        BuildPlayer(BuildTarget.StandaloneWindows64, filename[filename.Length - 1], path + "/");
    }

    // this is the main player builder function
    static void BuildPlayer(BuildTarget buildTarget, string filename, string path)
    {
        string fileExtension = "";
        string dataPath = "";
        string modifier = "";

        // configure path variables based on the platform we're targeting
        switch (buildTarget)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
//			modifier = "_windows";
                fileExtension = ".exe";
                dataPath = "_Data/";
                break;
            case BuildTarget.StandaloneOSXIntel:
            case BuildTarget.StandaloneOSXIntel64:
//		case BuildTarget.StandaloneOSXUniversal:
//			modifier = "_mac-osx";
//			fileExtension = ".app";
//			dataPath = fileExtension + "/Contents/";
//			break;
            case BuildTarget.StandaloneLinux:
            case BuildTarget.StandaloneLinux64:
            case BuildTarget.StandaloneLinuxUniversal:
                modifier = "_linux";
                dataPath = "_Data/";
                switch (buildTarget)
                {
                    case BuildTarget.StandaloneLinux:
                        fileExtension = ".x86";
                        break;
                    case BuildTarget.StandaloneLinux64:
                        fileExtension = ".x64";
                        break;
                    case BuildTarget.StandaloneLinuxUniversal:
                        fileExtension = ".x86_64";
                        break;
                }

                break;
        }

//        Debug.Log("====== BuildPlayer: " + buildTarget.ToString() + " at " + path + filename);
        EditorUserBuildSettings.SwitchActiveBuildTarget(buildTarget);

        // build out the player
        string buildPath = path + filename + modifier + "/";
//        Debug.Log("buildpath: " + buildPath);
        string playerPath = buildPath + filename + modifier + fileExtension;
//        Debug.Log("playerpath: " + playerPath);
        BuildPipeline.BuildPlayer(GetScenePaths(), playerPath, buildTarget,
            buildTarget == BuildTarget.StandaloneWindows64
                ? BuildOptions.ShowBuiltPlayer
                : BuildOptions.None);

        // Copy files over into builds
        string fullDataPath = buildPath + filename + modifier + dataPath;
//        Debug.Log("fullDataPath: " + fullDataPath);
//		CopyFromProjectAssets( fullDataPath, "languages"); // language text files that Radiator uses
        //  copy over readme

//        string zipTime = DateTime.Now.ToString(ZIP_TIME_FORMAT);
        // ZIP everything
//        var zipPath = path + "HahaRobotCoach.zip";
        var zipPath = path + BuildConfig.SchoolName + modifier + "_" + bundleVersion + ".zip";
        Debug.Log("Build finished at "+buildPath);
        CompressDirectory(buildPath, zipPath);
    }

    // from http://wiki.unity3d.com/index.php?title=AutoBuilder
    static string[] GetScenePaths()
    {
        string[] scenes = new string[EditorBuildSettings.scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
        {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }

        return scenes;
    }

    static string GetProjectName()
    {
        string[] s = Application.dataPath.Split('/');
        return s[s.Length - 2];
    }

    static string GetProjectFolderPath()
    {
        var s = Application.dataPath;
        s = s.Substring(0,s.Length - 7); // remove "Assets/"
        return s;
    }

    // copies over files from somewhere in my project folder to my standalone build's path
    // do not put a "/" at beginning of assetsFolderName
    static void CopyFromProjectAssets(string fullDataPath, string assetsFolderPath,
        bool deleteMetaFiles = true)
    {
//        Debug.Log("CopyFromProjectAssets: copying over " + assetsFolderPath);
        FileUtil.ReplaceDirectory(Application.dataPath + "/" + assetsFolderPath,
            fullDataPath + assetsFolderPath); // copy over languages

        // delete all meta files
        if (deleteMetaFiles)
        {
            var metaFiles = Directory.GetFiles(fullDataPath + assetsFolderPath, "*.meta",
                SearchOption.AllDirectories);
            foreach (var meta in metaFiles)
            {
                FileUtil.DeleteFileOrDirectory(meta);
            }
        }
    }

    // compress the folder into a ZIP file, uses https://github.com/r2d2rigo/dotnetzip-for-unity
    static void CompressDirectory(string directory, string zipFileOutputPath)
    {
//        Debug.Log("Attempting to compress " + directory + " into " + zipFileOutputPath);
        // display fake percentage, I can't get zip.SaveProgress event handler to work for some reason, whatever
        EditorUtility.DisplayProgressBar("COMPRESSING... please wait", zipFileOutputPath, 0.38f);
        using (ZipFile zip = new ZipFile())
        {
            zip.ParallelDeflateThreshold =
                -1; // DotNetZip bugfix that corrupts DLLs / binaries http://stackoverflow.com/questions/15337186/dotnetzip-badreadexception-on-extract
            zip.AddDirectory(directory);
            zip.Save(zipFileOutputPath);
        }

        EditorUtility.ClearProgressBar();

        Debug.Log("Compress finished at " + zipFileOutputPath);

    }
}
