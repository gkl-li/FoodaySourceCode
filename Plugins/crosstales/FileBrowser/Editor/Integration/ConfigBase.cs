﻿#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Crosstales.FB.EditorUtil;
using Crosstales.FB.EditorTask;

namespace Crosstales.FB.EditorIntegration
{
   /// <summary>Base class for editor windows.</summary>
   public abstract class ConfigBase : EditorWindow
   {
      #region Variables

      private static string updateText = UpdateCheck.TEXT_NOT_CHECKED;
      private static UpdateStatus updateStatus = UpdateStatus.NOT_CHECKED;

      private System.Threading.Thread worker;

      private Vector2 scrollPosConfig;
      private Vector2 scrollPosHelp;
      private Vector2 scrollPosAboutUpdate;
      private Vector2 scrollPosAboutReadme;
      private Vector2 scrollPosAboutVersions;

      private static string readme;
      private static string versions;

      private int aboutTab;

      private static readonly System.Random rnd = new System.Random();

      private readonly int adRnd1 = rnd.Next(0, 4);
      private readonly int adRnd2 = rnd.Next(0, 5);

      #endregion


      #region Protected methods

      protected void showConfiguration()
      {
         showPlatformNotSupported();

         scrollPosConfig = EditorGUILayout.BeginScrollView(scrollPosConfig, false, false);
         {
            GUILayout.Label("Global Settings", EditorStyles.boldLabel);

            EditorConfig.PREFAB_AUTOLOAD = EditorGUILayout.Toggle(new GUIContent("Prefab Auto-Load", $"Enable or disable auto-loading of the prefabs to the scene (default: {EditorConstants.DEFAULT_PREFAB_AUTOLOAD})."), EditorConfig.PREFAB_AUTOLOAD);

            Util.Config.DEBUG = EditorGUILayout.Toggle(new GUIContent("Debug", $"Enable or disable debug logs (default: {Util.Constants.DEFAULT_DEBUG})."), Util.Config.DEBUG);

            EditorConfig.UPDATE_CHECK = EditorGUILayout.Toggle(new GUIContent("Update Check", $"Enable or disable the update-checks for the asset (default: {EditorConstants.DEFAULT_UPDATE_CHECK})."), EditorConfig.UPDATE_CHECK);

            EditorConfig.COMPILE_DEFINES = EditorGUILayout.Toggle(new GUIContent("Compile Defines", $"Enable or disable adding compile defines 'CT_FB' and 'CT_FB_PRO' for the asset (default: {EditorConstants.DEFAULT_COMPILE_DEFINES})."), EditorConfig.COMPILE_DEFINES);

            EditorHelper.SeparatorUI();

            GUILayout.Label("FileBrowser", EditorStyles.boldLabel);
            EditorConfig.HIERARCHY_ICON = EditorGUILayout.Toggle(new GUIContent("Show Hierarchy Icon", $"Show hierarchy icon (default: {EditorConstants.DEFAULT_HIERARCHY_ICON})."), EditorConfig.HIERARCHY_ICON);

            EditorHelper.SeparatorUI();

            GUILayout.Label("Windows", EditorStyles.boldLabel);
            Util.Config.NATIVE_WINDOWS = EditorGUILayout.Toggle(new GUIContent("Native Inside Editor", $"Enable or disable native file browser inside the Unity Editor (default: {Util.Constants.DEFAULT_NATIVE_WINDOWS})."), Util.Config.NATIVE_WINDOWS);

            EditorHelper.SeparatorUI();

            GUILayout.Label("UWP (WSA)", EditorStyles.boldLabel);
            EditorConfig.MODIFY_MANIFEST = EditorGUILayout.Toggle(new GUIContent("Modify Manifest", "Modify the 'Package.appxmanifest' for builds to access all files of the system (default: " + EditorConstants.DEFAULT_MODIFY_MANIFEST + ")."), EditorConfig.MODIFY_MANIFEST);
         }
         EditorGUILayout.EndScrollView();
      }

      protected void showHelp()
      {
         showPlatformNotSupported();

         scrollPosHelp = EditorGUILayout.BeginScrollView(scrollPosHelp, false, false);
         {
            GUILayout.Label("Resources", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            {
               GUILayout.BeginVertical();
               {
                  if (GUILayout.Button(new GUIContent(" Manual", EditorHelper.Icon_Manual, "Show the manual.")))
                     Util.Helper.OpenURL(Util.Constants.ASSET_MANUAL_URL);

                  GUILayout.Space(6);

                  if (GUILayout.Button(new GUIContent(" Forum", EditorHelper.Icon_Forum, "Visit the forum page.")))
                     Util.Helper.OpenURL(Util.Constants.ASSET_FORUM_URL);
               }
               GUILayout.EndVertical();

               GUILayout.BeginVertical();
               {
                  if (GUILayout.Button(new GUIContent(" API", EditorHelper.Icon_API, "Show the API.")))
                     Util.Helper.OpenURL(Util.Constants.ASSET_API_URL);

                  GUILayout.Space(6);

                  if (GUILayout.Button(new GUIContent(" Product", EditorHelper.Icon_Product, "Visit the product page.")))
                     Util.Helper.OpenURL(Util.Constants.ASSET_WEB_URL);
               }
               GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            EditorHelper.SeparatorUI();

            GUILayout.Label("Videos", EditorStyles.boldLabel);
            /*
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(new GUIContent(" Promo", EditorHelper.Video_Promo, "View the promotion video on 'Youtube'.")))
                {
                    Util.Helper.OpenURL(Constants.ASSET_VIDEO_PROMO);
                }

                if (GUILayout.Button(new GUIContent(" Tutorial", EditorHelper.Video_Tutorial, "View the tutorial video on 'Youtube'.")))
                {
                    Util.Helper.OpenURL(Constants.ASSET_VIDEO_TUTORIAL);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            */

            if (GUILayout.Button(new GUIContent(" Tutorial", EditorHelper.Video_Tutorial, "View the tutorial video on 'Youtube'.")))
               Util.Helper.OpenURL(Util.Constants.ASSET_VIDEO_TUTORIAL);

            GUILayout.Space(6);

            if (GUILayout.Button(new GUIContent(" All Videos", EditorHelper.Icon_Videos, "Visit our 'Youtube'-channel for more videos.")))
               Util.Helper.OpenURL(Util.Constants.ASSET_SOCIAL_YOUTUBE);

            EditorHelper.SeparatorUI();

            GUILayout.Label("3rd Party Assets", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            {
               if (GUILayout.Button(new GUIContent(string.Empty, EditorHelper.Asset_PlayMaker, "More information about 'PlayMaker'.")))
                  Util.Helper.OpenURL(Util.Constants.ASSET_3P_PLAYMAKER);

               if (GUILayout.Button(new GUIContent(string.Empty, EditorHelper.Asset_RTFB, "More information about 'Runtime File Browser'.")))
                  Util.Helper.OpenURL(Util.Constants.ASSET_3P_RTFB);

               //CT Ads
               switch (adRnd1)
               {
                  case 0:
                  {
                     if (GUILayout.Button(new GUIContent(string.Empty, EditorHelper.Logo_Asset_BWF, "More information about 'Bad Word Filter'.")))
                        Util.Helper.OpenURL(Util.Constants.ASSET_BWF);

                     break;
                  }
                  case 1:
                  {
                     if (GUILayout.Button(new GUIContent(string.Empty, EditorHelper.Logo_Asset_DJ, "More information about 'DJ'.")))
                        Util.Helper.OpenURL(Util.Constants.ASSET_DJ);

                     break;
                  }
                  case 2:
                  {
                     if (GUILayout.Button(new GUIContent(string.Empty, EditorHelper.Logo_Asset_TR, "More information about 'True Random'.")))
                        Util.Helper.OpenURL(Util.Constants.ASSET_TR);

                     break;
                  }
                  default:
                  {
                     if (GUILayout.Button(new GUIContent(string.Empty, EditorHelper.Logo_Asset_TPB, "More information about 'Turbo Builder'.")))
                        Util.Helper.OpenURL(Util.Constants.ASSET_TPB);

                     break;
                  }
               }

               switch (adRnd2)
               {
                  case 0:
                  {
                     if (GUILayout.Button(new GUIContent(string.Empty, EditorHelper.Logo_Asset_Radio, "More information about 'Radio'.")))
                        Util.Helper.OpenURL(Util.Constants.ASSET_RADIO);

                     break;
                  }
                  case 1:
                  {
                     if (GUILayout.Button(new GUIContent(string.Empty, EditorHelper.Logo_Asset_TB, "More information about 'Turbo Backup'.")))
                        Util.Helper.OpenURL(Util.Constants.ASSET_TB);

                     break;
                  }
                  case 2:
                  {
                     if (GUILayout.Button(new GUIContent(string.Empty, EditorHelper.Logo_Asset_TPS, "More information about 'Turbo Switch'.")))
                        Util.Helper.OpenURL(Util.Constants.ASSET_TPS);

                     break;
                  }
                  case 3:
                  {
                     if (GUILayout.Button(new GUIContent(string.Empty, EditorHelper.Logo_Asset_RTV, "More information about 'RT-Voice'.")))
                        Util.Helper.OpenURL(Util.Constants.ASSET_RTV);

                     break;
                  }
                  default:
                  {
                     if (GUILayout.Button(new GUIContent(string.Empty, EditorHelper.Logo_Asset_OC, "More information about 'Online Check'.")))
                        Util.Helper.OpenURL(Util.Constants.ASSET_OC);

                     break;
                  }
               }
            }
            GUILayout.EndHorizontal();
         }
         EditorGUILayout.EndScrollView();

         GUILayout.Space(6);
      }

      protected void showAbout()
      {
         showPlatformNotSupported();

         GUILayout.Space(3);
         GUILayout.Label(Util.Constants.ASSET_NAME, EditorStyles.boldLabel);

         GUILayout.BeginHorizontal();
         {
            GUILayout.BeginVertical(GUILayout.Width(60));
            {
               GUILayout.Label("Version:");

               GUILayout.Space(12);

               GUILayout.Label("Web:");

               GUILayout.Space(2);

               GUILayout.Label("Email:");
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(170));
            {
               GUILayout.Space(0);

               GUILayout.Label(Util.Constants.ASSET_VERSION);

               GUILayout.Space(12);

               EditorGUILayout.SelectableLabel(Util.Constants.ASSET_AUTHOR_URL, GUILayout.Height(16), GUILayout.ExpandHeight(false));

               GUILayout.Space(2);

               EditorGUILayout.SelectableLabel(Util.Constants.ASSET_CONTACT, GUILayout.Height(16), GUILayout.ExpandHeight(false));
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            {
               //GUILayout.Space(0);
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(64));
            {
               if (GUILayout.Button(new GUIContent(string.Empty, EditorHelper.Logo_Asset, "Visit asset website")))
                  Util.Helper.OpenURL(EditorConstants.ASSET_URL);
            }
            GUILayout.EndVertical();
         }
         GUILayout.EndHorizontal();

         GUILayout.Label($"© 2017-2021 by {Util.Constants.ASSET_AUTHOR}");

         EditorHelper.SeparatorUI();

         GUILayout.BeginHorizontal();
         {
            if (GUILayout.Button(new GUIContent(" AssetStore", EditorHelper.Logo_Unity, "Visit the 'Unity AssetStore' website.")))
               Util.Helper.OpenURL(Util.Constants.ASSET_CT_URL);

            if (GUILayout.Button(new GUIContent($" {Util.Constants.ASSET_AUTHOR}", EditorHelper.Logo_CT, $"Visit the '{Util.Constants.ASSET_AUTHOR}' website.")))
               Util.Helper.OpenURL(Util.Constants.ASSET_AUTHOR_URL);
         }
         GUILayout.EndHorizontal();

         EditorHelper.SeparatorUI();

         aboutTab = GUILayout.Toolbar(aboutTab, new[] { "Readme", "Versions", "Update" });

         switch (aboutTab)
         {
            case 2:
            {
               scrollPosAboutUpdate = EditorGUILayout.BeginScrollView(scrollPosAboutUpdate, false, false);
               {
                  Color fgColor = GUI.color;

                  GUI.color = Color.yellow;

                  switch (updateStatus)
                  {
                     case UpdateStatus.NO_UPDATE:
                        GUI.color = Color.green;
                        GUILayout.Label(updateText);
                        break;
                     case UpdateStatus.UPDATE:
                     {
                        GUILayout.Label(updateText);

                        if (GUILayout.Button(new GUIContent(" Download", "Visit the 'Unity AssetStore' to download the latest version.")))
                           UnityEditorInternal.AssetStore.Open($"content/{EditorConstants.ASSET_ID}");

                        break;
                     }
                     case UpdateStatus.UPDATE_VERSION:
                     {
                        GUILayout.Label(updateText);

                        if (GUILayout.Button(new GUIContent(" Upgrade", "Upgrade to the newer version in the 'Unity AssetStore'")))
                           Util.Helper.OpenURL(Util.Constants.ASSET_CT_URL);

                        break;
                     }
                     case UpdateStatus.DEPRECATED:
                     {
                        GUILayout.Label(updateText);

                        if (GUILayout.Button(new GUIContent(" More Information", "Visit the 'crosstales'-site for more information.")))
                           Util.Helper.OpenURL(Util.Constants.ASSET_AUTHOR_URL);

                        break;
                     }
                     default:
                        GUI.color = Color.cyan;
                        GUILayout.Label(updateText);
                        break;
                  }

                  GUI.color = fgColor;
               }
               EditorGUILayout.EndScrollView();

               if (updateStatus == UpdateStatus.NOT_CHECKED || updateStatus == UpdateStatus.NO_UPDATE)
               {
                  bool isChecking = !(worker == null || worker?.IsAlive == false);

                  GUI.enabled = Util.Helper.isInternetAvailable && !isChecking;

                  if (GUILayout.Button(new GUIContent(isChecking ? "Checking... Please wait." : " Check For Update", EditorHelper.Icon_Check, $"Checks for available updates of {Util.Constants.ASSET_NAME}")))
                  {
                     worker = new System.Threading.Thread(() => UpdateCheck.UpdateCheckForEditor(out updateText, out updateStatus));
                     worker.Start();
                  }

                  GUI.enabled = true;
               }

               break;
            }
            case 0:
            {
               if (readme == null)
               {
                  string path = $"{Application.dataPath}{EditorConfig.ASSET_PATH}README.txt";

                  try
                  {
                     readme = System.IO.File.ReadAllText(path);
                  }
                  catch (System.Exception)
                  {
                     readme = $"README not found: {path}";
                  }
               }

               scrollPosAboutReadme = EditorGUILayout.BeginScrollView(scrollPosAboutReadme, false, false);
               {
                  GUILayout.Label(readme);
               }
               EditorGUILayout.EndScrollView();
               break;
            }
            default:
            {
               if (versions == null)
               {
                  string path = $"{Application.dataPath}{EditorConfig.ASSET_PATH}Documentation/VERSIONS.txt";

                  try
                  {
                     versions = System.IO.File.ReadAllText(path);
                  }
                  catch (System.Exception)
                  {
                     versions = $"VERSIONS not found: {path}";
                  }
               }

               scrollPosAboutVersions = EditorGUILayout.BeginScrollView(scrollPosAboutVersions, false, false);
               {
                  GUILayout.Label(versions);
               }

               EditorGUILayout.EndScrollView();
               break;
            }
         }

         EditorHelper.SeparatorUI();

         GUILayout.BeginHorizontal();
         {
            if (GUILayout.Button(new GUIContent(string.Empty, EditorHelper.Social_Discord, "Communicate with us via 'Discord'.")))
               Util.Helper.OpenURL(Util.Constants.ASSET_SOCIAL_DISCORD);

            if (GUILayout.Button(new GUIContent(string.Empty, EditorHelper.Social_Facebook, "Follow us on 'Facebook'.")))
               Util.Helper.OpenURL(Util.Constants.ASSET_SOCIAL_FACEBOOK);

            if (GUILayout.Button(new GUIContent(string.Empty, EditorHelper.Social_Twitter, "Follow us on 'Twitter'.")))
               Util.Helper.OpenURL(Util.Constants.ASSET_SOCIAL_TWITTER);

            if (GUILayout.Button(new GUIContent(string.Empty, EditorHelper.Social_Linkedin, "Follow us on 'LinkedIn'.")))
               Util.Helper.OpenURL(Util.Constants.ASSET_SOCIAL_LINKEDIN);
         }
         GUILayout.EndHorizontal();

         GUILayout.Space(6);
      }

      protected static void save()
      {
         Util.Config.Save();
         EditorConfig.Save();

         if (Util.Config.DEBUG)
            Debug.Log("Config data saved");
      }

      protected void showPlatformNotSupported()
      {
         if (EditorHelper.isFileBrowserInScene && !FileBrowser.Instance.isPlatformSupported)
         {
            EditorGUILayout.HelpBox("The current platform is not supported by the File Browser. Please add a custom wrapper (e.g. Runtime File Browser).", MessageType.Error);
         }
      }

      #endregion
   }
}
#endif
// © 2019-2021 crosstales LLC (https://www.crosstales.com)