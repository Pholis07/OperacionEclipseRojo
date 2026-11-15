using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Build WebGL con las escenas del Build Settings, compresión Gzip y Decompression Fallback.
/// Menú Herramientas > Build WebGL, o en modo batch:
/// Unity -batchmode -nographics -quit -projectPath &lt;proyecto&gt; -buildTarget WebGL -executeMethod ConstructorWebGL.Construir
/// La salida va a Build/WebGL (carpeta ignorada por git).
/// </summary>
public static class ConstructorWebGL
{
    const string Salida = "Build/WebGL";

    [MenuItem("Herramientas/Build WebGL")]
    public static void Construir()
    {
        // Plantilla propia: el juego ocupa toda la ventana (la de Unity fija 1920x1080 y se corta en pantallas pequeñas).
        PlayerSettings.WebGL.template = "PROJECT:PantallaCompleta";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        AplicarSplash();

        var escenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var reporte = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = escenas,
            locationPathName = Salida,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });

        var r = reporte.summary;
        Debug.Log("ConstructorWebGL: " + r.result + ", " + (r.totalSize / (1024f * 1024f)).ToString("0.0") +
                  " MB, " + r.totalErrors + " errores, " + r.totalTime);
        if (Application.isBatchMode && r.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    /// <summary>
    /// Splash al abrir el juego: fondo del juego (#0B0B10), primero los dos logos del patrocinador y después
    /// «Made with Unity». Se aplica en cada build para que no se pierda si alguien toca Player Settings.
    /// </summary>
    [MenuItem("Herramientas/Aplicar splash del patrocinador")]
    public static void AplicarSplash()
    {
        var patrocinio = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arte/Patrocinio/Patrocinio_Splash.png");
        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.showUnityLogo = true;
        PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
        PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
        PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
        PlayerSettings.SplashScreen.backgroundColor = new Color32(11, 11, 16, 255);
        PlayerSettings.SplashScreen.overlayOpacity = 0f;
        PlayerSettings.SplashScreen.logos = patrocinio != null
            ? new[] { PlayerSettings.SplashScreenLogo.Create(2.6f, patrocinio), PlayerSettings.SplashScreenLogo.CreateWithUnityLogo(2f) }
            : new[] { PlayerSettings.SplashScreenLogo.CreateWithUnityLogo(2f) };
        AssetDatabase.SaveAssets();
        Debug.Log("ConstructorWebGL: splash con " + (patrocinio != null ? "patrocinador y Unity" : "solo Unity (falta Patrocinio_Splash.png)"));
    }
}
