using UnityEngine;
using UnityEditor;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;

public class ScormExporterAdvanced : EditorWindow
{
    private string buildURL = "./";
    private string identifier = "UnitySCORMTest";
    private string title = "Unity WebGL SCORM Test";
    private string indexScormFile = "index_scorm.html";
    private string wrapperFile = "SCORMWrapper.js";
    private bool openFolderAfterExport = true;

    // 🔹 NEW: Toggle to host elsewhere or same LMS
    private bool hostElsewhere = false;

    [MenuItem("Tools/SCORM Packaging/Export SCORM Package")]
    public static void ShowWindow()
    {
        GetWindow<ScormExporterAdvanced>("SCORM Exporter Advanced");
    }

    void OnGUI()
    {
        GUILayout.Label("SCORM Export Settings", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Configure your SCORM metadata and build hosting details below.", MessageType.Info);

        GUILayout.Space(8);
        GUILayout.Label("SCORM Configuration", EditorStyles.boldLabel);
        identifier = EditorGUILayout.TextField(new GUIContent("Identifier", "Unique SCORM package identifier"), identifier);
        title = EditorGUILayout.TextField(new GUIContent("Title", "Title displayed in LMS"), title);

        GUILayout.Space(8);
        GUILayout.Label("Hosting Option", EditorStyles.boldLabel);
        hostElsewhere = EditorGUILayout.Toggle(new GUIContent("Host Build on External Server", "Enable if your WebGL build is hosted elsewhere (two zips will be generated). Disable to include everything in one SCORM zip."), hostElsewhere);

        // 🔹 Only show URL if hosting elsewhere
        if (hostElsewhere)
        {
            EditorGUILayout.HelpBox("Enter the base URL where your WebGL build will be hosted.", MessageType.None);
            buildURL = EditorGUILayout.TextField(new GUIContent("Hosted Build URL", "URL where the WebGL game is hosted"), buildURL);
        }

        GUILayout.Space(8);
        GUILayout.Label("File Settings", EditorStyles.boldLabel);
        indexScormFile = EditorGUILayout.TextField(new GUIContent("Index Scorm File", "The HTML file that loads your SCORM content"), indexScormFile);
        wrapperFile = EditorGUILayout.TextField(new GUIContent("JS Wrapper File", "The JavaScript SCORM wrapper file"), wrapperFile);

        GUILayout.Space(10);
        openFolderAfterExport = EditorGUILayout.Toggle(new GUIContent("Open Folder After Export", "Automatically open the export folder once done"), openFolderAfterExport);

        GUILayout.Space(15);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUIStyle buildButton = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold,
            fixedWidth = 280,
            fixedHeight = 40,
            fontSize = 13
        };

        Color defaultColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.22f, 0.55f, 0.95f);

        if (GUILayout.Button("Build & Export SCORM Package", buildButton))
        {
            BuildAndExportScorm();
        }

        GUI.backgroundColor = defaultColor;
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(15);
    }

    void BuildAndExportScorm()
    {
        string buildFolder = EditorUtility.SaveFolderPanel("Select WebGL Build Output Folder", "", "");
        if (string.IsNullOrEmpty(buildFolder))
            return;

        string buildName = Path.GetFileName(buildFolder);
        if (string.IsNullOrEmpty(buildName))
        {
            EditorUtility.DisplayDialog("Error", "Invalid folder name. Please select a proper folder.", "OK");
            return;
        }

        Debug.Log($"Starting WebGL build for: <b>{buildName}</b>...");
        string[] scenes = GetEnabledScenes();
        BuildPipeline.BuildPlayer(scenes, buildFolder, BuildTarget.WebGL, BuildOptions.None);
        Debug.Log("Unity WebGL Build Complete!");

        // 🔹 1. Determine iframe source based on toggle
        string iframeSrc = hostElsewhere
            ? $"{buildURL.TrimEnd('/')}/{buildName}/index.html"
            : "index.html"; // for LMS build-included version

        // 🔹 2. Generate index_scorm.html
        string htmlPath = Path.Combine(buildFolder, indexScormFile);
        string htmlContent = $@"<!DOCTYPE html>
<html lang=""en"">
  <head>
    <meta name=""viewport"" content=""initial-scale=1, minimum-scale=1, maximum-scale=1"" />
    <meta charset=""utf-8"">
    <title>{title}</title>
    <style>
      html, body, div, iframe {{
        margin: 0;
        padding: 0;
        height: 100%;
        border: none;
      }}
      iframe {{
        display: block;
        width: 100%;
        height: 100%;
        border: none;
      }}
    </style>
    <script src=""{wrapperFile}""></script>
  </head>

  <body>
    <iframe id=""gameFrame"" src=""{iframeSrc}""></iframe>
    <script>
      window.addEventListener(""message"", (event) => {{
        if (event.data && event.data.type === ""unityReady"") {{
          console.log(""Unity iframe is ready"");
          var gameFrame = document.getElementById(""gameFrame"");
        }}
      }});
    </script>
  </body>
</html>";

        File.WriteAllText(htmlPath, htmlContent, Encoding.UTF8);
        Debug.Log("🧾 index_scorm.html generated dynamically.");

        // 🔹 3. Create imsmanifest.xml
        string manifestPath = Path.Combine(buildFolder, "imsmanifest.xml");
        string manifest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest identifier=""{identifier}"" version=""1.2""
 xmlns=""http://www.imsproject.org/xsd/imscp_rootv1p1p2""
 xmlns:adlcp=""http://www.adlnet.org/xsd/adlcp_rootv1p2""
 xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
 xsi:schemaLocation=""http://www.imsproject.org/xsd/imscp_rootv1p1p2 imscp_rootv1p1p2.xsd
                     http://www.adlnet.org/xsd/adlcp_rootv1p2 adlcp_rootv1p2.xsd"">
	<organizations default=""ORG1"">
		<organization identifier=""ORG1"">
			<title>{title}</title>
			<item identifier=""ITEM1"" identifierref=""RES1"">
				<title>{title}</title>
			</item>
		</organization>
	</organizations>

	<resources>
		<resource identifier=""RES1"" type=""webcontent"" adlcp:scormtype=""sco"" href=""{indexScormFile}"">
			<file href=""{indexScormFile}""/>
			<file href=""{wrapperFile}""/>
		</resource>
	</resources>
</manifest>";

        File.WriteAllText(manifestPath, manifest, Encoding.UTF8);
        Debug.Log("🧩 imsmanifest.xml created successfully.");

        // 🔹 4. Zip creation based on toggle
        if (hostElsewhere)
        {
            // ✅ Two zips (SCORM + Content)
            CreateTwoZips(buildFolder, buildName, indexScormFile, wrapperFile, manifestPath);
        }
        else
        {
            // ✅ Single SCORM zip (all in one)
            CreateSingleZip(buildFolder, buildName);
        }

        if (openFolderAfterExport)
        {
            EditorUtility.RevealInFinder(buildFolder);
        }
    }

    // 🔹 NEW: Single SCORM zip containing everything
    void CreateSingleZip(string buildFolder, string buildName)
    {
        string zipPath = Path.Combine(Path.GetDirectoryName(buildFolder)!, $"{buildName}_scorm_full.zip");
        if (File.Exists(zipPath)) File.Delete(zipPath);

        ZipFile.CreateFromDirectory(buildFolder, zipPath);
        EditorUtility.DisplayDialog("SCORM Export Complete",
            $"Single SCORM Package Created:\n{zipPath}", "OK");

        Debug.Log($"Single SCORM Package Created: {zipPath}");
    }

    // 🔹 Existing logic for two-zip setup
    void CreateTwoZips(string buildFolder, string buildName, string indexScormFile, string wrapperFile, string manifestPath)
    {
        string scormZipPath = Path.Combine(Path.GetDirectoryName(buildFolder)!, $"{buildName}_scorm.zip");
        string scormTemp = Path.Combine(Path.GetTempPath(), "UnitySCORM_CoreTemp");
        if (Directory.Exists(scormTemp)) Directory.Delete(scormTemp, true);
        Directory.CreateDirectory(scormTemp);

        File.Copy(manifestPath, Path.Combine(scormTemp, "imsmanifest.xml"), true);
        File.Copy(Path.Combine(buildFolder, indexScormFile), Path.Combine(scormTemp, indexScormFile), true);
        File.Copy(Path.Combine(buildFolder, wrapperFile), Path.Combine(scormTemp, wrapperFile), true);

        if (File.Exists(scormZipPath)) File.Delete(scormZipPath);
        ZipFile.CreateFromDirectory(scormTemp, scormZipPath);

        // content zip
        string contentZipPath = Path.Combine(Path.GetDirectoryName(buildFolder)!, $"{buildName}.zip");
        string contentTemp = Path.Combine(Path.GetTempPath(), "UnitySCORM_ContentTemp");
        if (Directory.Exists(contentTemp)) Directory.Delete(contentTemp, true);
        Directory.CreateDirectory(contentTemp);

        string[] excludedFiles = { "imsmanifest.xml", indexScormFile, wrapperFile };
        foreach (string file in Directory.GetFiles(buildFolder, "*", SearchOption.AllDirectories))
        {
            string fileName = Path.GetFileName(file);
            if (excludedFiles.Contains(fileName)) continue;

            string relativePath = file.Substring(buildFolder.Length + 1);
            string destPath = Path.Combine(contentTemp, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(file, destPath, true);
        }

        if (File.Exists(contentZipPath)) File.Delete(contentZipPath);
        ZipFile.CreateFromDirectory(contentTemp, contentZipPath);

        Directory.Delete(scormTemp, true);
        Directory.Delete(contentTemp, true);

        EditorUtility.DisplayDialog("SCORM Export Complete",
            $"SCORM Zip: {scormZipPath}\n\nGame Zip: {contentZipPath}",
            "OK");

        Debug.Log($"Export complete!\n➡️ SCORM Core: {scormZipPath}\n➡️ Game Content: {contentZipPath}");
    }

    private string[] GetEnabledScenes()
    {
        return System.Array.FindAll(EditorBuildSettings.scenes, s => s.enabled)
                           .Select(s => s.path)
                           .ToArray();
    }
}

