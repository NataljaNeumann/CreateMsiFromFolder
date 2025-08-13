/* MIT License

Copyright (c) 2025 NataljaNeumann

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;

namespace CreateMsiFromFolder
{
    class Program
    {
        public static void Main(string[] aArgs)
        {
            // Input parameters
            string releaseDir = Environment.CurrentDirectory;
            string outputDir = Directory.GetParent(releaseDir).FullName;
            string licenseFilePath = Path.Combine(releaseDir, "license.txt");
            string applicationName = "MyApplication";
            string companyName = "GNU";
            string version = "1.0.0.0";
            string upgradeCodeGuid = Guid.NewGuid().ToString();

            // Extract metadata from one of the .NET executables in the release directory
            var exeFile = Directory.GetFiles(releaseDir, "*.exe").FirstOrDefault();
            if (exeFile == null)
            {
                Console.WriteLine("No .exe files found in the release directory.");
                return;
            }

            Console.WriteLine("Extracting metadata from: " + exeFile);
            ExtractMetadata(exeFile, ref applicationName, ref companyName, ref version);

            // Parse command line arguments
            for (int i = 0; i < aArgs.Length; i++)
            {
                switch (aArgs[i])
                {
                    case "--releasedir":
                        if (i + 1 < aArgs.Length)
                        {
                            releaseDir = aArgs[++i];
                            licenseFilePath = Path.Combine(releaseDir, "license.txt");
                            outputDir = Directory.GetParent(releaseDir).FullName;

                            // Extract metadata from one of the .NET executables in the release directory
                            var exeFile2 = Directory.GetFiles(releaseDir, "*.exe").FirstOrDefault();
                            if (exeFile2 == null)
                            {
                                Console.WriteLine("No .exe files found in the release directory.");
                                return;
                            }

                            ExtractMetadata(exeFile, ref applicationName, ref companyName, ref version);
                        }
                        break;
                    case "--outputdir":
                        if (i + 1 < aArgs.Length)
                        {
                            outputDir = aArgs[++i];
                        }
                        break;
                    case "--licencefile":
                        if (i + 1 < aArgs.Length)
                        {
                            licenseFilePath = aArgs[++i];
                            if (!licenseFilePath.Contains("\\"))
                                licenseFilePath = releaseDir + licenseFilePath;
                        }
                        break;
                    case "--applicationname":
                        if (i + 1 < aArgs.Length)
                        {
                            applicationName = aArgs[++i];
                        }
                        break;
                    case "--companyname":
                        if (i + 1 < aArgs.Length)
                        {
                            companyName = aArgs[++i];
                        }
                        break;
                    case "--guid":
                        if (i + 1 < aArgs.Length)
                        {
                            upgradeCodeGuid = aArgs[++i];
                        }
                        break;
                    case "--version":
                        if (i + 1 < aArgs.Length)
                        {
                            version = aArgs[++i];
                        }
                        break;
                    case "--help":
                    case "-?":
                    case "/?":
                        try
                        {
                            System.Diagnostics.Process.Start(System.IO.Path.Combine(
                                AppDomain.CurrentDomain.BaseDirectory,
                                "Readme.html"));
                        }
                        catch
                        {
                            // ignore
                        }
                        goto case "--licence";
                    case "--licence":
                    case "--license":
                        Version oVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                        WriteWrappedText(
                            string.Format("CreateMsiFromFolder v{0}.{1} {2} {3}",
                            oVersion.Major, oVersion.Minor,
                            Properties.Resources.CopyrightMessage, "NataljaNeumann@gmx.de"));
                        System.Console.WriteLine();
                        WriteWrappedText(Properties.Resources.LicenseText);
                        System.Console.WriteLine();
                        WriteWrappedText(
                            //Properties.Resources.ForConversionToCsv +
                            "CreateMsiFromFolder --directory <dir> --pattern <pattern> [--sortbyname yes] [--onlystrings no]\r\n" +
                            "  [--addcultures <comma-separated-list>]");
                        WriteWrappedText(
                            //Properties.Resources.ForTranslation +
                            "CreateMsiFromFolder --directory <dir> --pattern <pattern> \r\n" +
                            "  --translator <google|microsoft|deepl|toptranslation> --apikey <key> [--sortbyname yes]");
                        WriteWrappedText(
                            //Properties.Resources.ForTranslationWithArgos +
                            "CreateMsiFromFolder --directory <dir> --pattern <pattern> \r\n" +
                            "  --translator argos [--sortbyname yes]");
                        WriteWrappedText(
                            //Properties.Resources.ForTranslationWithLibreTranslate +
                            "CreateMsiFromFolder --directory <dir> --pattern <pattern> \r\n" +
                            "  --translator libretranslate --libreurl <url> [--apikey <key>] [--sortbyname yes]");
                        WriteWrappedText(
                            //Properties.Resources.ForUpdatingResxFiles +
                            "CreateMsiFromFolder --directory <dir> --toresx <resources.csv> [--removeduplicates yes]\r\n" +
                            "  [--addcultures <comma-separated-list>]");
                        return;
                }
            }


            // Validate WiX tools availability
            if (!CheckWiXToolsAvailability())
            {
                Console.WriteLine("WiX Toolset is not installed. Please install it from https://wixtoolset.org/");
                return;
            }

            // Validate license file
            if (!File.Exists(licenseFilePath))
            {
                Console.WriteLine("License file not found at: " + licenseFilePath);
                return;
            }

            // Create output directory
            Directory.CreateDirectory(outputDir);

            // Generate .wxs file content
            string wxsContent = GenerateWxsFileContent(releaseDir, licenseFilePath, applicationName, companyName, upgradeCodeGuid);

            // Save the .wxs file
            string wxsFilePath = Path.Combine(outputDir, applicationName + "-Installer.wxs");
            File.WriteAllText(wxsFilePath, wxsContent);
            Console.WriteLine("Generated WXS file at: " + wxsFilePath);

            // Compile and link using WiX tools
            CompileAndLinkWiX(wxsFilePath, outputDir, applicationName);
        }

        //===================================================================================================
        /// <summary>
        /// Writes wrapped text to console
        /// </summary>
        /// <param name="strText">Text to write</param>
        /// <param name="nWindowWidth">Window width</param>
        /// <param name="bRightAligned">Indicates, if the text shall appear right-aligned</param>
        //===================================================================================================
        static void WriteWrappedText(
            string strText
            )
        {
            // for some languages we don't wrap the text, because it is unclear where the words start and end
            if (!Properties.Resources.UseWordWrap.Equals("Yes", StringComparison.InvariantCultureIgnoreCase))
            {
                System.Console.WriteLine(strText);
                return;
            }

            // first split the lines, if there are several lines
            if (strText.Contains("\n"))
            {
                // then write each line, wrapped
                foreach (string strLine in strText.Replace(Environment.NewLine, "\n").Split('\n'))
                {
                    WriteWrappedText(strLine.TrimEnd());
                }
            }
            else
            {
                // get somme information about the width of the window
                int nWindowWidth = Console.WindowWidth - 1;
                // and if we need to align the text at right, e.g. hebrew or arabic
                bool bRightToLeft = Properties.Resources.RightToLeft.Equals("Yes",
                    StringComparison.InvariantCultureIgnoreCase);

                // now process all words separately
                string[] aWords = strText.Split(' ');
                string strCurrentLine = "";

                foreach (string strWord in aWords)
                {
                    if ((strCurrentLine + strWord).Length < nWindowWidth)
                    {
                        strCurrentLine += strWord + " ";
                    }
                    else
                    {


                        if (bRightToLeft && !strCurrentLine.StartsWith("ResxVsCsv") &&
                            !strCurrentLine.StartsWith("  --") && !strCurrentLine.StartsWith("  [--"))
                        {
                            strCurrentLine = ReverseArabicAndHebrewText(strCurrentLine);

                            strCurrentLine = new string(' ', nWindowWidth - strCurrentLine.Length)
                                + (strCurrentLine.TrimEnd());
                        }
                        else
                            if (bRightToLeft)
                                strCurrentLine = ReverseArabicAndHebrewParts(strCurrentLine);

                        Console.WriteLine(strCurrentLine);

                        strCurrentLine = strWord + " ";
                    }
                }

                // the remaining part of the string
                if (strCurrentLine.Length > 0)
                {

                    if (bRightToLeft && !strCurrentLine.StartsWith("ResxVsCsv") &&
                        !strCurrentLine.StartsWith("  --") && !strCurrentLine.StartsWith("  [--"))
                    {
                        strCurrentLine = ReverseArabicAndHebrewText(strCurrentLine);

                        strCurrentLine = new string(' ', nWindowWidth - strCurrentLine.Length)
                                       + (strCurrentLine.TrimEnd());
                    }
                    else
                        if (bRightToLeft)
                            strCurrentLine = ReverseArabicAndHebrewParts(strCurrentLine);


                    Console.WriteLine(strCurrentLine);
                }
            }
        }


        //===================================================================================================
        /// <summary>
        /// This reverses the complete text, except non-arabic and non-hebrew parts, useful for rtl languages
        /// </summary>
        /// <param name="strInput">Input text</param>
        /// <returns>Reversed text</returns>
        //===================================================================================================
        static string ReverseArabicAndHebrewText(string strInput)
        {
            // first reverse the complete line
            char[] aChars = strInput.ToCharArray();
            Array.Reverse(aChars);
            for (int i = aChars.Length - 1; i >= 0; --i)
                switch (aChars[i])
                {
                    case '(':
                        aChars[i] = ')';
                        break;
                    case ')':
                        aChars[i] = '(';
                        break;
                    case '/':
                        aChars[i] = '\\';
                        break;
                    case '\\':
                        aChars[i] = '/';
                        break;
                }
            string strAllReversed = new string(aChars);

            // now reverse non-arabic and non-hebrew back to the normal order
            string strPattern = "[^\u0590-\u05FF\u0600-\u06FF:\\s()\\\"]+";
            return Regex.Replace(strAllReversed, strPattern, new MatchEvaluator(ReverseMatch));
        }


        //===================================================================================================
        /// <summary>
        /// This reverses hebrew and arabic parts, useful for ltr languages
        /// </summary>
        /// <param name="strInput">Input text</param>
        /// <returns>Text with reversed hebrew and arabic parts</returns>
        //===================================================================================================
        static string ReverseArabicAndHebrewParts(string strInput)
        {
            string strPattern = "[\\u0590-\\u05FF\\u0600-\\u06FF\\s,:\\.\\\"\\'\\(\\)]+";
            return Regex.Replace(strInput, strPattern, new MatchEvaluator(ReverseMatch));
        }


        //===================================================================================================
        /// <summary>
        /// Reverses a regex match
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        //===================================================================================================
        static string ReverseMatch(Match oMatch)
        {
            char[] aChars = oMatch.Value.ToCharArray();
            Array.Reverse(aChars);
            for (int i = aChars.Length - 1; i >= 0; --i)
                switch (aChars[i])
                {
                    case '(':
                        aChars[i] = ')';
                        break;
                    case ')':
                        aChars[i] = '(';
                        break;
                    case '/':
                        aChars[i] = '\\';
                        break;
                    case '\\':
                        aChars[i] = '/';
                        break;
                }
            return new string(aChars);
        }


        static bool CheckWiXToolsAvailability()
        {
            return File.Exists("candle.exe") && File.Exists("light.exe");
        }


        static void ExtractMetadata(string exeFile, ref string appName, ref string company, ref string version)
        {
            try
            {
                var assembly = Assembly.LoadFrom(exeFile);

                // Get Product Name
                var productAttribute = assembly.GetCustomAttribute<AssemblyProductAttribute>();
                if (productAttribute != null && !string.IsNullOrEmpty(productAttribute.Product))
                    appName = productAttribute.Product;

                // Get Company Name
                var companyAttribute = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
                if (companyAttribute != null && !string.IsNullOrEmpty(companyAttribute.Company))
                    company = companyAttribute.Company;

                // Get Version
                var versionAttribute = assembly.GetCustomAttribute<AssemblyVersionAttribute>();
                if (versionAttribute != null && !string.IsNullOrEmpty(versionAttribute.Version))
                    version = versionAttribute.Version;

                // Get Copyright
                var copyrightAttribute = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
                if (copyrightAttribute != null)
                {
                    Console.WriteLine("Copyright: " + copyrightAttribute.Copyright);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error extracting metadata: " + ex.Message);
            }
        }


        static string GenerateWxsFileContent(string releaseDir, string licenseFilePath, string applicationName, string companyName, string upgradeCodeGuid)
        {
            var wxsBuilder = new System.Text.StringBuilder();
            wxsBuilder.AppendLine("<?xml version='1.0'?>");
            wxsBuilder.AppendLine("<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>");

            // Product Definition
            wxsBuilder.AppendLine("  <Product Id='*' Name='" + applicationName + "' Language='1033' Version='1.0.0.0' Manufacturer='" + companyName + "' UpgradeCode='" + upgradeCodeGuid + "'>");
            wxsBuilder.AppendLine("    <Package InstallerVersion='200' Compressed='yes' />");
            wxsBuilder.AppendLine("    <MajorUpgrade DowngradeErrorMessage='A newer version of [ProductName] is already installed.' />");
            wxsBuilder.AppendLine("    <MediaTemplate />");

            // Define properties
            wxsBuilder.AppendLine("    <Property Id='ALLUSERS' Secure='yes' />");
            wxsBuilder.AppendLine("    <Property Id='WixUI_InstallScope' Value='perMachine' />");

            // UI References
            wxsBuilder.AppendLine("    <UI>");
            wxsBuilder.AppendLine("      <UIRef Id='WixUI_License' />");
            wxsBuilder.AppendLine("      <UIRef Id='WixUI_InstallDir' />");
            wxsBuilder.AppendLine("      <DialogRef Id=\"CustomInstallScopeDialog\" />");
            wxsBuilder.AppendLine("      <Publish Dialog=\"VerifyReadyDlg\" Control=\"Back\" Event=\"NewDialog\" Value=\"CustomInstallScopeDialog\">1</Publish>");
            wxsBuilder.AppendLine("      <UIRef Id='WixUI_ErrorProgressText' />");
            wxsBuilder.AppendLine("    </UI>");

            // License File
            wxsBuilder.AppendLine("    <WixVariable Id='WixUILicenseRtf' Value='" + licenseFilePath + "' />");

            // Feature Definition
            wxsBuilder.AppendLine("    <Feature Id='DefaultFeature' Title='Main Feature' Level='1'>");
            wxsBuilder.AppendLine("      <ComponentGroupRef Id='FilesGroup' />");
            wxsBuilder.AppendLine("      <ComponentGroupRef Id='ShortcutsGroup' />");
            wxsBuilder.AppendLine("    </Feature>");

            wxsBuilder.AppendLine("  </Product>");

            // Directory Definition
            wxsBuilder.AppendLine("  <Fragment>");
            wxsBuilder.AppendLine("    <Directory Id='TARGETDIR' Name='SourceDir'>");
            wxsBuilder.AppendLine("      <Directory Id='ProgramFilesFolder'>");
            wxsBuilder.AppendLine("        <Directory Id='INSTALLFOLDER' Name='" + applicationName + "' />");
            wxsBuilder.AppendLine("      </Directory>");
            wxsBuilder.AppendLine("      <Directory Id='ProgramMenuFolder'>");
            wxsBuilder.AppendLine("        <Directory Id='ProgramMenuSubFolder' Name='" + applicationName + "' />");
            wxsBuilder.AppendLine("      </Directory>");
            wxsBuilder.AppendLine("      <Directory Id='DesktopFolder' />");
            wxsBuilder.AppendLine("    </Directory>");
            wxsBuilder.AppendLine("  </Fragment>");

            // Add application files
            wxsBuilder.AppendLine("  <Fragment>");
            wxsBuilder.AppendLine("    <ComponentGroup Id='FilesGroup' Directory='INSTALLFOLDER'>");
            foreach (var file in Directory.GetFiles(releaseDir, "*.*", SearchOption.AllDirectories))
            {
                string relativePath = file.Substring(releaseDir.Length).TrimStart('\\');
                wxsBuilder.AppendLine("      <Component Id='File_" + Path.GetFileName(file) + "' Guid='*'>");
                wxsBuilder.AppendLine("        <File Source='" + file + "' KeyPath='yes' />");
                wxsBuilder.AppendLine("      </Component>");
            }
            wxsBuilder.AppendLine("    </ComponentGroup>");
            wxsBuilder.AppendLine("  </Fragment>");

            // Add shortcuts
            wxsBuilder.AppendLine("  <Fragment>");
            wxsBuilder.AppendLine("    <ComponentGroup Id='ShortcutsGroup'>");
            var exeFiles = Directory.GetFiles(releaseDir, "*.exe", SearchOption.TopDirectoryOnly);
            foreach (var exeFile in exeFiles)
            {
                string exeName = Path.GetFileNameWithoutExtension(exeFile);
                string shortcutName = SplitCamelCase(exeName);
                wxsBuilder.AppendLine("      <Component Id='Shortcut_" + exeName + "' Guid='*'>");
                wxsBuilder.AppendLine("        <Shortcut Id='StartMenuShortcut_" + exeName + "' Name='" + shortcutName + "' Target='[INSTALLFOLDER]" + Path.GetFileName(exeFile) + "' />");
                wxsBuilder.AppendLine("        <RegistryValue Root='HKCU' Key='Software\\{companyName}\\" + applicationName + "' Name='Shortcut_" + exeName + "' Type='string' Value='yes' KeyPath='yes' />");
                wxsBuilder.AppendLine("      </Component>");
            }
            wxsBuilder.AppendLine("    </ComponentGroup>");
            wxsBuilder.AppendLine("  </Fragment>");

            wxsBuilder.Append("<Fragment>");
            wxsBuilder.Append("    <UI>");
            wxsBuilder.Append("      <!-- Custom Install Scope Dialog -->");
            wxsBuilder.Append("      <Dialog Id=\"CustomInstallScopeDialog\" Width=\"370\" Height=\"270\" Title=\"[ProductName] Setup\">");
            wxsBuilder.Append("        <Control Id=\"Description\" Type=\"Text\" X=\"20\" Y=\"20\" Width=\"330\" Height=\"40\" Transparent=\"yes\" NoPrefix=\"yes\">");
            wxsBuilder.Append("          <Text>Select the installation type:</Text>");
            wxsBuilder.Append("        </Control>");
            wxsBuilder.Append("        <Control Id=\"PerMachine\" Type=\"RadioButtonGroup\" X=\"20\" Y=\"60\" Width=\"330\" Height=\"40\">");
            wxsBuilder.Append("          <RadioButtonGroup Property=\"ALLUSERS\">");
            wxsBuilder.Append("            <RadioButton Value=\"1\" X=\"0\" Y=\"0\" Width=\"300\" Height=\"17\" Text=\"Install for all users (requires admin privileges)\" />");
            wxsBuilder.Append("            <RadioButton Value=\"0\" X=\"0\" Y=\"20\" Width=\"300\" Height=\"17\" Text=\"Install for only me (current user)\" />");
            wxsBuilder.Append("          </RadioButtonGroup>");
            wxsBuilder.Append("        </Control>");
            wxsBuilder.Append("        <Control Id=\"Next\" Type=\"PushButton\" X=\"236\" Y=\"243\" Width=\"60\" Height=\"17\" Default=\"yes\" Cancel=\"yes\" Text=\"Next\">");
            wxsBuilder.Append("          <Publish Event=\"EndDialog\" Value=\"Return\">1</Publish>");
            wxsBuilder.Append("        </Control>");
            wxsBuilder.Append("        <Control Id=\"Cancel\" Type=\"PushButton\" X=\"304\" Y=\"243\" Width=\"60\" Height=\"17\" Cancel=\"yes\" Text=\"Cancel\">");
            wxsBuilder.Append("          <Publish Event=\"EndDialog\" Value=\"Exit\">1</Publish>");
            wxsBuilder.Append("        </Control>");
            wxsBuilder.Append("      </Dialog>");
            wxsBuilder.Append("    </UI>");
            wxsBuilder.Append("  </Fragment>");

            wxsBuilder.AppendLine("</Wix>");
            return wxsBuilder.ToString();
        }

        static string SplitCamelCase(string input)
        {
            return Regex.Replace(input, "([a-z])([A-Z])", "$1 $2");
        }

        static void CompileAndLinkWiX(string wxsFilePath, string outputDir, string applicationName)
        {
            string wxsFileName = Path.GetFileNameWithoutExtension(wxsFilePath);
            string wixObjFilePath = Path.Combine(outputDir, wxsFileName + ".wixobj");
            string msiFilePath = Path.Combine(outputDir, applicationName + ".msi");

            string enWxlFile = Path.Combine(outputDir, "en-us.wxl");
            string deWxlFile = Path.Combine(outputDir, "de-de.wxl");

            Console.WriteLine("Compiling with WiX candle.exe...");
            System.Diagnostics.Process.Start("candle.exe", "-o \"" + wixObjFilePath + "\" \"" + wxsFilePath + "\"").WaitForExit();

            Console.WriteLine("Linking with WiX light.exe...");
            System.Diagnostics.Process.Start("light.exe", "-out \"" + msiFilePath + "\" \"" + wixObjFilePath + "\" -loc \"" + enWxlFile + "\" -loc \"" + deWxlFile + "\"").WaitForExit();

            Console.WriteLine("Installer created at: " + msiFilePath);
        }
    }

}
