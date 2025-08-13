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
            string strReleaseDir = Environment.CurrentDirectory;
            string strOutputDir = Directory.GetParent(strReleaseDir).FullName;
            string strLicenseFilePath = Path.Combine(strReleaseDir, "license.txt");
            string strApplicationName = "MyApplication";
            string strCompanyName = "GNU";
            string strVersion = "1.0.0.0";
            string strUpgradeCodeGuid = Guid.NewGuid().ToString();
            bool bOnlyXml = false;

            // Extract metadata from one of the .NET executables in the release directory
            var exeFile = Directory.GetFiles(strReleaseDir, "*.exe").FirstOrDefault();
            if (exeFile == null)
            {
                Console.WriteLine("No .exe files found in the release directory.");
                return;
            }

            Console.WriteLine("Extracting metadata from: " + exeFile);
            ExtractMetadata(exeFile, ref strApplicationName, ref strCompanyName, ref strVersion);

            // Parse command line arguments
            for (int i = 0; i < aArgs.Length; i++)
            {
                switch (aArgs[i])
                {
                    case "--releasedir":
                        if (i + 1 < aArgs.Length)
                        {
                            strReleaseDir = aArgs[++i];
                            strLicenseFilePath = Path.Combine(strReleaseDir, "license.txt");
                            strOutputDir = Directory.GetParent(strReleaseDir).FullName;

                            // Extract metadata from one of the .NET executables in the release directory
                            var exeFile2 = Directory.GetFiles(strReleaseDir, "*.exe").FirstOrDefault();
                            if (exeFile2 == null)
                            {
                                Console.WriteLine("No .exe files found in the release directory.");
                                return;
                            }

                            ExtractMetadata(exeFile2, ref strApplicationName, ref strCompanyName, ref strVersion);
                        }
                        break;
                    case "--outputdir":
                        if (i + 1 < aArgs.Length)
                        {
                            strOutputDir = aArgs[++i];
                        }
                        break;
                    case "--licencefile":
                        if (i + 1 < aArgs.Length)
                        {
                            strLicenseFilePath = aArgs[++i];
                            if (!strLicenseFilePath.Contains("\\\\") && !strLicenseFilePath.Contains(":"))
                                strLicenseFilePath = Path.Combine(strReleaseDir, strLicenseFilePath);
                        }
                        break;
                    case "--applicationname":
                        if (i + 1 < aArgs.Length)
                        {
                            strApplicationName = aArgs[++i];
                        }
                        break;
                    case "--companyname":
                        if (i + 1 < aArgs.Length)
                        {
                            strCompanyName = aArgs[++i];
                        }
                        break;
                    case "--guid":
                        if (i + 1 < aArgs.Length)
                        {
                            strUpgradeCodeGuid = aArgs[++i];
                        }
                        break;
                    case "--version":
                        if (i + 1 < aArgs.Length)
                        {
                            strVersion = aArgs[++i];
                        }
                        break;
                    case "--onlyxml":
                    case "--nomsi":
                        if (i + 1 < aArgs.Length)
                        {
                            bOnlyXml = aArgs[++i].Equals("yes");
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
            if (!bOnlyXml && !CheckWiXToolsAvailability())
            {
                Console.WriteLine("WiX Toolset is not installed. Please install it from https://wixtoolset.org/");
                return;
            }

            // Validate license file
            if (!File.Exists(strLicenseFilePath))
            {
                Console.WriteLine("License file not found at: " + strLicenseFilePath);
                return;
            }

            // Create output directory
            Directory.CreateDirectory(strOutputDir);

            // Generate .wxs file content
            string wxsContent = GenerateWxsFileContent(strReleaseDir, strLicenseFilePath, strApplicationName, strCompanyName, strUpgradeCodeGuid);

            // Save the .wxs file
            string wxsFilePath = Path.Combine(strOutputDir, strApplicationName + "-Installer.wxs");
            File.WriteAllText(wxsFilePath, wxsContent);
            Console.WriteLine("Generated WXS file at: " + wxsFilePath);

            // Compile and link using WiX tools
            CompileAndLinkWiX(wxsFilePath, strOutputDir, strApplicationName);
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

        //===================================================================================================
        /// <summary>
        /// Tests, if a file can be executed from path variable
        /// </summary>
        /// <param name="strFileName">Name of the file to execute</param>
        /// <returns>true iff the file is in directories of the path variable</returns>
        //===================================================================================================
        static bool IsExecutableInPath(
            string strFileName
            )
        {
            string strPathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(strPathEnv))
                return false;

            string[] aPaths = strPathEnv.Split(';');
            foreach (string strPath in aPaths)
            {
                try
                {
                    string strFullPath = Path.Combine(strPath.Trim(), strFileName);
                    if (File.Exists(strFullPath))
                        return true;
                }
                catch
                {
                    // Ignore invalid paths
                }
            }
            return false;
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if executables can be executed
        /// </summary>
        /// <returns>true iff candle and light can be executed</returns>
        //===================================================================================================
        static bool CheckWiXToolsAvailability()
        {
            return (File.Exists("candle.exe") && File.Exists("light.exe") ) ||
                (IsExecutableInPath("candle.exe") && IsExecutableInPath("light.exe"));
        }


        //===================================================================================================
        /// <summary>
        /// Extracts metadata from an assembly
        /// </summary>
        /// <param name="strExeFile">The exe file of the assemmbly</param>
        /// <param name="rstrAppName">In/Out application name</param>
        /// <param name="rstrCompany">In/Out company name</param>
        /// <param name="rstrVersion">In/Out version</param>
        //===================================================================================================
        static void ExtractMetadata(
            string strExeFile, 
            ref string rstrAppName, 
            ref string rstrCompany, 
            ref string rstrVersion)
        {
            try
            {
                var oAssembly = Assembly.LoadFrom(strExeFile);

                // Get Product Name
                var oProductAttribute = oAssembly.GetCustomAttribute<AssemblyProductAttribute>();
                if (oProductAttribute != null && !string.IsNullOrEmpty(oProductAttribute.Product))
                    rstrAppName = oProductAttribute.Product;

                // Get Company Name
                var oCompanyAttribute = oAssembly.GetCustomAttribute<AssemblyCompanyAttribute>();
                if (oCompanyAttribute != null && !string.IsNullOrEmpty(oCompanyAttribute.Company))
                    rstrCompany = oCompanyAttribute.Company;

                // Get Version
                var oVersionAttribute = oAssembly.GetCustomAttribute<AssemblyVersionAttribute>();
                if (oVersionAttribute != null && !string.IsNullOrEmpty(oVersionAttribute.Version))
                    rstrVersion = oVersionAttribute.Version;
                else
                {
                    Regex oRegex = new Regex(".*Version=([\\.0-9A-Za-z]*).*");
                    Match oMatch = oRegex.Match(oAssembly.FullName);
                    if (oMatch.Success)
                    {
                        GroupCollection oGroups = oMatch.Groups;
                        if (oGroups.Count > 1 && oGroups[1].Success)
                        {
                            Group oGroup = oGroups[1];
                            rstrVersion = oGroup.Value;
                        }
                    }
                }



                // Get Copyright
                var oCopyrightAttribute = oAssembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
                if (oCopyrightAttribute != null)
                {
                    Console.WriteLine("Copyright: " + oCopyrightAttribute.Copyright);
                }
            }
            catch (Exception oEx)
            {
                Console.WriteLine("Error extracting metadata: " + oEx.Message);
            }
        }


        static string GenerateWxsFileContent(string releaseDir, string licenseFilePath, string applicationName, string companyName, string upgradeCodeGuid)
        {
            System.Text.StringBuilder oWxsBuilder = new System.Text.StringBuilder();
            oWxsBuilder.AppendLine("<?xml version='1.0'?>");
            oWxsBuilder.AppendLine("<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>");

            // Product Definition
            oWxsBuilder.AppendLine("  <Product Id='*' Name='" + applicationName + "' Language='1033' Version='1.0.0.0' Manufacturer='" + companyName + "' UpgradeCode='" + upgradeCodeGuid + "'>");
            oWxsBuilder.AppendLine("    <Package InstallerVersion='200' Compressed='yes' />");
            oWxsBuilder.AppendLine("    <MajorUpgrade DowngradeErrorMessage='A newer version of [ProductName] is already installed.' />");
            oWxsBuilder.AppendLine("    <MediaTemplate />");

            // Define properties
            oWxsBuilder.AppendLine("    <Property Id='ALLUSERS' Secure='yes' />");
            oWxsBuilder.AppendLine("    <Property Id='WixUI_InstallScope' Value='perMachine' />");

            // UI References
            oWxsBuilder.AppendLine("    <UI>");
            oWxsBuilder.AppendLine("      <UIRef Id='WixUI_License' />");
            oWxsBuilder.AppendLine("      <UIRef Id='WixUI_InstallDir' />");
            oWxsBuilder.AppendLine("      <DialogRef Id=\"CustomInstallScopeDialog\" />");
            oWxsBuilder.AppendLine("      <Publish Dialog=\"VerifyReadyDlg\" Control=\"Back\" Event=\"NewDialog\" Value=\"CustomInstallScopeDialog\">1</Publish>");
            oWxsBuilder.AppendLine("      <UIRef Id='WixUI_ErrorProgressText' />");
            oWxsBuilder.AppendLine("    </UI>");

            // License File
            oWxsBuilder.AppendLine("    <WixVariable Id='WixUILicenseRtf' Value='" + licenseFilePath + "' />");

            // Feature Definition
            oWxsBuilder.AppendLine("    <Feature Id='DefaultFeature' Title='Main Feature' Level='1'>");
            oWxsBuilder.AppendLine("      <ComponentGroupRef Id='FilesGroup' />");
            oWxsBuilder.AppendLine("      <ComponentGroupRef Id='ShortcutsGroup' />");
            oWxsBuilder.AppendLine("    </Feature>");

            oWxsBuilder.AppendLine("  </Product>");

            // Directory Definition
            oWxsBuilder.AppendLine("  <Fragment>");
            oWxsBuilder.AppendLine("    <Directory Id='TARGETDIR' Name='SourceDir'>");
            oWxsBuilder.AppendLine("      <Directory Id='ProgramFilesFolder'>");
            oWxsBuilder.AppendLine("        <Directory Id='INSTALLFOLDER' Name='" + applicationName + "' />");
            oWxsBuilder.AppendLine("      </Directory>");
            oWxsBuilder.AppendLine("      <Directory Id='ProgramMenuFolder'>");
            oWxsBuilder.AppendLine("        <Directory Id='ProgramMenuSubFolder' Name='" + applicationName + "' />");
            oWxsBuilder.AppendLine("      </Directory>");
            oWxsBuilder.AppendLine("      <Directory Id='DesktopFolder' />");
            oWxsBuilder.AppendLine("    </Directory>");
            oWxsBuilder.AppendLine("  </Fragment>");

            // Add application files
            oWxsBuilder.AppendLine("  <Fragment>");
            oWxsBuilder.AppendLine("    <ComponentGroup Id='FilesGroup' Directory='INSTALLFOLDER'>");
            foreach (var strFullFilePath in Directory.GetFiles(releaseDir, "*.*", SearchOption.AllDirectories))
            {
                string strRelativePath = strFullFilePath.Substring(releaseDir.Length).TrimStart('\\');
                oWxsBuilder.AppendLine("      <Component Id='File_" + 
                    (strRelativePath.Replace("\\","__").Replace(".","_")) + "' Guid='*'>");
                oWxsBuilder.AppendLine("        <File Source='" + strFullFilePath + "' KeyPath='yes' />");
                oWxsBuilder.AppendLine("      </Component>");
            }
            oWxsBuilder.AppendLine("    </ComponentGroup>");
            oWxsBuilder.AppendLine("  </Fragment>");

            // Add shortcuts
            oWxsBuilder.AppendLine("  <Fragment>");
            oWxsBuilder.AppendLine("    <ComponentGroup Id='ShortcutsGroup'>");
            var oExeFiles = Directory.GetFiles(releaseDir, "*.exe", SearchOption.TopDirectoryOnly);
            foreach (var strExeFile in oExeFiles)
            {
                string strExeName = Path.GetFileNameWithoutExtension(strExeFile);
                string strShortcutName = SplitCamelCase(strExeName);
                oWxsBuilder.AppendLine("      <Component Id='Shortcut_" + strExeName + "' Guid='*'>");
                oWxsBuilder.AppendLine("        <Shortcut Id='StartMenuShortcut_" + strExeName + "' Name='" + strShortcutName + "' Target='[INSTALLFOLDER]" + Path.GetFileName(strExeFile) + "' />");
                oWxsBuilder.AppendLine("        <RegistryValue Root='HKCU' Key='Software\\"+companyName+"\\" + applicationName + "' Name='Shortcut_" + strExeName + "' Type='string' Value='yes' KeyPath='yes' />");
                oWxsBuilder.AppendLine("      </Component>");
            }
            oWxsBuilder.AppendLine("    </ComponentGroup>");
            oWxsBuilder.AppendLine("  </Fragment>");

            oWxsBuilder.AppendLine("<Fragment>");
            oWxsBuilder.AppendLine("    <UI>");
            oWxsBuilder.AppendLine("      <!-- Custom Install Scope Dialog -->");
            oWxsBuilder.AppendLine("      <Dialog Id=\"CustomInstallScopeDialog\" Width=\"370\" Height=\"270\" Title=\"[ProductName] Setup\">");
            oWxsBuilder.AppendLine("        <Control Id=\"Description\" Type=\"Text\" X=\"20\" Y=\"20\" Width=\"330\" Height=\"40\" Transparent=\"yes\" NoPrefix=\"yes\">");
            oWxsBuilder.AppendLine("          <Text>Select the installation type:</Text>");
            oWxsBuilder.AppendLine("        </Control>");
            oWxsBuilder.AppendLine("        <Control Id=\"PerMachine\" Type=\"RadioButtonGroup\" X=\"20\" Y=\"60\" Width=\"330\" Height=\"40\">");
            oWxsBuilder.AppendLine("          <RadioButtonGroup Property=\"ALLUSERS\">");
            oWxsBuilder.AppendLine("            <RadioButton Value=\"1\" X=\"0\" Y=\"0\" Width=\"300\" Height=\"17\" Text=\"Install for all users (requires admin privileges)\" />");
            oWxsBuilder.AppendLine("            <RadioButton Value=\"0\" X=\"0\" Y=\"20\" Width=\"300\" Height=\"17\" Text=\"Install for only me (current user)\" />");
            oWxsBuilder.AppendLine("          </RadioButtonGroup>");
            oWxsBuilder.AppendLine("        </Control>");
            oWxsBuilder.AppendLine("        <Control Id=\"Next\" Type=\"PushButton\" X=\"236\" Y=\"243\" Width=\"60\" Height=\"17\" Default=\"yes\" Cancel=\"yes\" Text=\"Next\">");
            oWxsBuilder.AppendLine("          <Publish Event=\"EndDialog\" Value=\"Return\">1</Publish>");
            oWxsBuilder.AppendLine("        </Control>");
            oWxsBuilder.AppendLine("        <Control Id=\"Cancel\" Type=\"PushButton\" X=\"304\" Y=\"243\" Width=\"60\" Height=\"17\" Cancel=\"yes\" Text=\"Cancel\">");
            oWxsBuilder.AppendLine("          <Publish Event=\"EndDialog\" Value=\"Exit\">1</Publish>");
            oWxsBuilder.AppendLine("        </Control>");
            oWxsBuilder.AppendLine("      </Dialog>");
            oWxsBuilder.AppendLine("    </UI>");
            oWxsBuilder.AppendLine("  </Fragment>");

            oWxsBuilder.AppendLine("</Wix>");
            return oWxsBuilder.ToString();
        }

        //===================================================================================================
        /// <summary>
        /// Splits camel case to single words
        /// </summary>
        /// <param name="input"></param>
        /// <returns>Camelcase split to single words</returns>
        //===================================================================================================
        static string SplitCamelCase(string strCamelCaseInput)
        {
            return Regex.Replace(strCamelCaseInput, "([a-z])([A-Z])", "$1 $2");
        }


        static void CompileAndLinkWiX(
            string strWxsFilePath, 
            string strOutputDir, 
            string strApplicationName
            )
        {
            string strWxsFileName = Path.GetFileNameWithoutExtension(strWxsFilePath);
            string strWixObjFilePath = Path.Combine(strOutputDir, strWxsFileName + ".wixobj");
            string strMsiFilePath = Path.Combine(strOutputDir, strApplicationName + ".msi");

            string strEnWxlFile = Path.Combine(strOutputDir, "en-us.wxl");
            string strDeWxlFile = Path.Combine(strOutputDir, "de-de.wxl");

            Console.WriteLine("Compiling with WiX candle.exe...");
            System.Diagnostics.Process.Start("candle.exe", "-o \"" + strWixObjFilePath + "\" \"" + strWxsFilePath + "\"").WaitForExit();

            Console.WriteLine("Linking with WiX light.exe...");
            System.Diagnostics.Process.Start("light.exe", "-out \"" + strMsiFilePath + "\" \"" + strWixObjFilePath + "\" -loc \"" + strEnWxlFile + "\" -loc \"" + strDeWxlFile + "\"").WaitForExit();

            Console.WriteLine("Installer created at: " + strMsiFilePath);
        }
    }

}
