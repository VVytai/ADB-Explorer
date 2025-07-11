﻿using ADB_Explorer.Helpers;
using ADB_Explorer.Models;
using ADB_Explorer.Services;

namespace ADB_Explorer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static string SettingsFilePath;
    private static readonly JsonSerializerSettings JsonSettings = new() { TypeNameHandling = TypeNameHandling.Objects };

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Read to force it to be set to Windows' culture
        _ = Data.Settings.OriginalCulture;

        if (e.Args.Length > 0)
        {
            if (!Directory.Exists(e.Args[0]))
            {
                MessageBox.Show($"{Strings.Resources.S_PATH_INVALID}\n\n{e.Args[0]}", Strings.Resources.S_CUSTOM_DATA_PATH, MessageBoxButton.OK, MessageBoxImage.Error);
                
                Current.Shutdown(1);
                return;
            }

            Data.AppDataPath = e.Args[0];
        }
        else
            Data.AppDataPath = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), "AppData", "Local", AdbExplorerConst.APP_DATA_FOLDER);

        SettingsFilePath = FileHelper.ConcatPaths(Data.AppDataPath, AdbExplorerConst.APP_SETTINGS_FILE, '\\');
        
        try
        {
            // if settings file exists in local app data - try to read it from there, otherwise try to read it from the isolated storage (old method)
            if (File.Exists(SettingsFilePath))
            {
                using StreamReader appDataReader = new(SettingsFilePath);
                ReadSettingsFile(appDataReader);
            }
            else
            {
                if (!Directory.Exists(Data.AppDataPath))
                    Directory.CreateDirectory(Data.AppDataPath);

                using IsolatedStorageFileStream stream = new(AdbExplorerConst.APP_SETTINGS_FILE,
                                                             FileMode.Open,
                                                             IsolatedStorageFile.GetUserStoreForDomain());
                using StreamReader reader = new(stream);
                ReadSettingsFile(reader);
            }

            if (!Data.Settings.UICulture.Equals(CultureInfo.InvariantCulture))
            {
                Thread.CurrentThread.CurrentUICulture =
                Thread.CurrentThread.CurrentCulture = Data.Settings.UICulture;
            }
            
#if !DEPLOY
            if (!File.Exists(ADB_Explorer.Properties.AppGlobal.DragDropLogPath))
            {
                File.WriteAllText(ADB_Explorer.Properties.AppGlobal.DragDropLogPath, "");
            }
#endif

        }
        catch
        {
            // in any case of failing to read the settings, try to write them instead
            // will happen on first ever launch, or after resetting app settings

            WriteSettings();
        }

        //Select the text in a TextBox when it receives focus.
        EventManager.RegisterClassHandler(typeof(TextBox), TextBox.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler(SelectivelyIgnoreMouseButton));
        EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotKeyboardFocusEvent,
            new RoutedEventHandler(SelectAllText));
        EventManager.RegisterClassHandler(typeof(TextBox), TextBox.MouseDoubleClickEvent,
            new RoutedEventHandler(SelectAllText));


        void ReadSettingsFile(StreamReader reader)
        {
            while (!reader.EndOfStream)
            {
                string[] keyValue = reader.ReadLine().TrimEnd(';').Split(':', 2);
                try
                {
                    var jObj = JsonConvert.DeserializeObject(keyValue[1], JsonSettings);
                    if (jObj is JArray jArr)
                        Properties[keyValue[0]] = jArr.Values<string>().ToArray();
                    else
                        Properties[keyValue[0]] = jObj;
                }
                catch (Exception)
                {
                    Properties[keyValue[0]] = keyValue[1];
                }
            }
        }

        ClearDrag();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        Data.FileOpQ.Stop();
        WriteSettings();

        if (Data.Settings.UnrootOnDisconnect is true)
            ADBService.Unroot(Data.CurrentADBDevice);

        App.Current.Dispatcher.Invoke(ClearDrag);
    }

    private static void ClearDrag()
    {
        try
        {
            Directory.GetDirectories(Data.AppDataPath).ForEach(dir => Directory.Delete(dir, true));
        }
        catch
        { }
    }

    private void WriteSettings()
    {
        if (Data.RuntimeSettings.ResetAppSettings)
        {
            try
            {
                File.Delete(SettingsFilePath);
            }
            catch
            { }
            
            return;
        }

        try
        {
            using StreamWriter writer = new(SettingsFilePath);

            foreach (string key in from string key in Properties.Keys
                                   orderby key
                                   select key)
            {
                writer.WriteLine($"{key}:{JsonConvert.SerializeObject(Properties[key], JsonSettings)};");
            }
        }
        catch (Exception)
        { }
    }

    private void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
    {
        // Find the TextBox
        DependencyObject parent = e.OriginalSource as UIElement;
        while (parent is not null and not TextBox)
            parent = VisualTreeHelper.GetParent(parent);

        if (parent is not null)
        {
            var textBox = (TextBox)parent;
            if (!textBox.IsKeyboardFocusWithin)
            {
                // If the text box is not yet focused, give it the focus and
                // stop further processing of this click event.
                textBox.Focus();
                e.Handled = true;
            }
        }
    }

    private void SelectAllText(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
            tb.SelectAll();
    }

    private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Handle error 0x800401D0 (CLIPBRD_E_CANT_OPEN) - global WPF issue
        if (e.Exception is COMException comException && comException.ErrorCode == -2147221040)
            e.Handled = true;

        // If application shutdown has started, do not throw exceptions
        if (App.Current is null || App.Current.Dispatcher is null)
            e.Handled = true;
    }
}
