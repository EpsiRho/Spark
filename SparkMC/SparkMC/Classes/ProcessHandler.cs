using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkMC.Classes
{
    public static class ProcessHandler
    {
        public static Process proc { get; set; }
        public static IntPtr hwnd { get; set; }
        public static async Task<string> GetError()
        {
            try
            {
                if (proc.HasExited)
                {
                    return $"[SPARK] Server process has closed ({proc.ExitCode})\n{await proc.StandardError.ReadToEndAsync()}";
                }
                else
                {
                    string err = $"[SPARK] {await proc.StandardError.ReadToEndAsync()}";
                    if (err != "[SPARK] ")
                    {
                        return err;
                    }
                    else
                    {
                        return "[SPARK] Unknown Error";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"[SPARK] {ex.Message}";
            }
        }
        public static async Task<string> GetStandardOut()
        {
            try
            {
                string stdOut = await proc.StandardOutput.ReadLineAsync();

                if (stdOut != null && stdOut != "")
                {
                    return stdOut;
                }
                else
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                return $"[SPARK] {ex.Message}";
            }
        }
        public static async Task<bool> SendToStandardIn(string input)
        {
            try
            {
                await proc.StandardInput.WriteLineAsync(input);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public static async void ParseJarInput()
        {

        }

    }
}
