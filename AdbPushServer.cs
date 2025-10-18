using System;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Windows;

namespace Gamepad.Touchpad
{
    internal static class AdbPushServer
	{   
        public const string RemotePath = "/data/local/tmp/xtmapper-server-touchpad.apk";
        
        public static bool IsServerInstalled(string serial) 
        {
            string serverPath = GetServerPath();
            if (serverPath == null)
            {
                return false;
            }
            
            if (!File.Exists(serverPath))
            {
                MessageBox.Show("Server APK file missing. Re-install the application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
                        
            // Get local file MD5
            string localMd5 = GetLocalFileMd5(serverPath);
            if (localMd5 == null)
            {
                MessageBox.Show("Failed to compute local file MD5", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            
            // Get remote file MD5
            string remoteMd5 = GetRemoteFileMd5(serial, RemotePath);
            
            if (remoteMd5 == null)
            {
                MessageBox.Show("Failed to compute remote file MD5", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // If remote file exists and MD5 matches, no need to push
            if (remoteMd5 == localMd5)
            {
                return true;
            } else {
                return false;
            }
        }

        public static bool PushServer(string serial)
        {
            string serverPath = GetServerPath();
            if (serverPath == null)
            {
                return false;
            }
           
            bool ok = AdbHelper.AdbPush(serial, serverPath, RemotePath);
            return ok;
        }

        static string GetServerPath()
        {
            try
            {
                // Get the program's local directory
                string localDirectory = AppDomain.CurrentDomain.BaseDirectory;
                
                // Combine with the filename
                string serverPath = Path.Combine(localDirectory, "xtmapper-server-touchpad.apk");
                
                return serverPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting server path: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private static string GetLocalFileMd5(string filePath)
        {
            try
            {
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error computing local MD5: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private static string GetRemoteFileMd5(string serial, string remotePath)
        {
            try
            {
                // Use adb shell to get MD5 sum of remote file
                string command = $"shell md5sum {remotePath}";
                string output = AdbHelper.RunAdbCommand(serial, command);
                
                if (string.IsNullOrEmpty(output))
                    return null;
                    
                // MD5 sum output format: "md5sum filename"
                string[] parts = output.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 1)
                {
                    // Extract just the MD5 hash (first 32 characters)
                    string md5 = parts[0].Trim();
                    if (md5.Length == 32)
                        return md5.ToLowerInvariant();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting remote MD5: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }
}