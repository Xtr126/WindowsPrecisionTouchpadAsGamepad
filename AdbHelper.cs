using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.ComponentModel;

namespace RawInput.Touchpad
{
    internal class AdbHelper
    {
        public static bool GetConnectedDevices(List<AdbDevice> outVec)
        {
            try
            {
                // Prepare the adb process
                var psi = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = "devices -l",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                // Execute adb command
                using (var process = Process.Start(psi))
                {
                    if (process == null) {
                        MessageBox.Show(
                            "Failed to start adb process.",
                            "Process start failed",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                        
                        return false;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        
                        MessageBox.Show(
                            $"adb exited with code {process.ExitCode}: {error}",
                            "ADB Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                        
                        return false;
                    }

                    MessageBox.Show(
                            output,
                            $"adb exited with code {process.ExitCode}",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                    );
                    // Pass output to parser
                    return AdbDevicesParser.ParseDevices(output, outVec);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{ex.Message}",
                    "Error executing adb",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return false;
            }
        }

        public static string RunAdbCommand(string serial, string command)
        {
            try
            {
                string fullCommand = string.IsNullOrEmpty(serial) 
                    ? command 
                    : $"-s {serial} {command}";

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = fullCommand,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using (Process process = Process.Start(startInfo))
                {
                    if (process == null) {
                        MessageBox.Show(
                            "Failed to start adb process.",
                            "Process start failed",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                        
                        return null;
                    }
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    if (process.ExitCode != 0)
                    {
                        MessageBox.Show(
                            $"adb exited with code {process.ExitCode}: {error}",
                            "ADB Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                        return null;
                    }
                    
                    return output.Trim();
                }
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 2) // File not found
            {
                MessageBox.Show(
                    $"{ex.Message}",
                    "Error: adb.exe not found.",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{ex.Message}",
                    "Error executing adb",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return null;
            }
        }
    }
}