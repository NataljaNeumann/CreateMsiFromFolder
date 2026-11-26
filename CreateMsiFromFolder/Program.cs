/* MIT License

Copyright (c) 2025-2026 NataljaNeumann@gmx.de

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

using System.Collections.Generic;

using System.Diagnostics;

using System.IO;
using System.Linq;
using System.Reflection;

using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace CreateMsiFromFolder
{
    //*******************************************************************************************************
    /// <summary>
    /// This program creates a .wixproj and a .wxs file
    /// </summary>
    //*******************************************************************************************************
    class Program
    {
        //===================================================================================================
        /// <summary>
        /// Main method of the application
        /// </summary>
        /// <param name="astrArgs">Command line arguments</param>
        //===================================================================================================
        public static void Main(string[] astrArgs)
        {
            // Input parameters
            string strWixProjectFile = "";
            string strStartupProject = "";
            string strWxsFile = "";
            string strReleaseDir = Environment.CurrentDirectory;
            string strOutputDir = Directory.GetParent(strReleaseDir).FullName;
            string strLicenseFileName = "license.rtf";
            string strApplicationName = "MyApplication";
            string strCompanyName = "GNU";
            string strVersion = "1.0.0.0";
            bool bSkipPdb = true;
            string strUpgradeCodeGuid = Guid.NewGuid().ToString().ToUpper();
            bool bVersioning = false;


            if (astrArgs.Length == 0)
            {
                astrArgs = new string[] { "--license" };
            }
            else
            {
                if (astrArgs.FirstOrDefault(x => x.Equals("--releasedir")) == null)
                {
                    // Extract metadata from one of the .NET executables in the release directory
                    var exeFile = Directory.GetFiles(strReleaseDir, "*.exe").FirstOrDefault();
                    if (exeFile == null)
                    {
                        Console.WriteLine("No .exe files found in current directory.");
                        return;
                    }

                    ExtractMetadata(exeFile, ref strApplicationName, ref strCompanyName, ref strVersion, false);
                }
            }

            // Parse command line arguments
            for (int i = 0; i < astrArgs.Length; i++)
            {
                switch (astrArgs[i])
                {
                    case "--wixproj":
                        if (i+1 < astrArgs.Length)
                        {
                            strWixProjectFile = astrArgs[++i];
                        }
                        break;
                    case "--startupproj":
                        if (i+1 < astrArgs.Length)
                        {
                            strStartupProject = astrArgs[++i];
                            if (strWixProjectFile.Equals(""))
                            {
                                strWixProjectFile =
                                    Path.GetFileNameWithoutExtension(strStartupProject) + ".Setup.wixproj";
                            }
                        }
                        break;
                    case "--wxsfile":
                        if (i+1 < astrArgs.Length)
                        {
                            strWxsFile = astrArgs[++i];
                        }
                        break;
                    case "--releasedir":
                        if (i + 1 < astrArgs.Length)
                        {
                            strReleaseDir = astrArgs[++i];
                            // Extract metadata from one of the .NET executables in the release directory
                            var strExeFile2 = Directory.GetFiles(strReleaseDir, "*.exe").FirstOrDefault();
                            if (strExeFile2 == null)
                            {
                                Console.WriteLine("No .exe files found in the release directory.");
                                return;
                            }

                            ExtractMetadata(strExeFile2, ref strApplicationName, ref strCompanyName, ref strVersion, true);

                            if (strStartupProject.Equals(""))
                            {
                                strStartupProject = strReleaseDir
                                    .Replace("\\Debug", "")
                                    .Replace("\\Release", "")
                                    .Replace("\\bin", "")
                                    .Replace("\\net8.0", "")
                                    .Replace("\\net8.0-windows", "");
                                strStartupProject = strStartupProject.Substring(strStartupProject.LastIndexOf("\\") + 1);

                                if (strWixProjectFile.Equals(""))
                                {
                                    strWixProjectFile =
                                        Path.GetFileNameWithoutExtension(strStartupProject) + ".Setup.wixproj";
                                }
                            }
                        }
                        break;
                    case "--skippdb":
                        if (i+1 < astrArgs.Length)
                        {
                            bSkipPdb = "yes".Equals(astrArgs[++i]);
                        }
                        break;
                    case "--outputdir":
                        if (i + 1 < astrArgs.Length)
                        {
                            strOutputDir = astrArgs[++i];
                        }
                        break;
                    case "--licencefile":
                        if (i + 1 < astrArgs.Length)
                        {
                            strLicenseFileName = astrArgs[++i];
                        }
                        break;
                    case "--applicationname":
                        if (i + 1 < astrArgs.Length)
                        {
                            strApplicationName = astrArgs[++i];
                        }
                        break;
                    case "--companyname":
                        if (i + 1 < astrArgs.Length)
                        {
                            strCompanyName = astrArgs[++i];
                        }
                        break;
                    case "--guid":
                        if (i + 1 < astrArgs.Length)
                        {
                            strUpgradeCodeGuid = astrArgs[++i];
                        }
                        break;
                    case "--version":
                        if (i + 1 < astrArgs.Length)
                        {
                            strVersion = astrArgs[++i];
                        }
                        break;
                    case "--versioning":
                        if (i + 1 < astrArgs.Length)
                        {
                            bVersioning = astrArgs[++i].Equals("yes");
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
                            "CreateMsiFromFolder --releasedir <dir> --outputdir <..\\Template.Setup> --wixproj <template.setup.wixproj> --startupproj <template.csproj> --wxsfile <template.wxs> --licencefile license.rtf --applicationname <ApplicationName>");
                        System.Console.WriteLine();
                        WriteWrappedText(
                            "CreateMsiFromFolder  --releasedir . --outputdir ..\\..\\..\\CreateMsiFromFolder.Setup --wixproj CreateMsiFromFolder.Setup.wixproj --startupproj CreateMsiFromFolder.csproj  --wxsfile Product.wxs --licencefile license.rtf --applicationname CreateMsiFromFolder --skippdb yes");
                        System.Console.ReadKey();

                        return;
                }
            }

            // Validate license file
            if (!File.Exists(Path.Combine(strReleaseDir, strLicenseFileName)))
            {
                Console.WriteLine("License file not found at: " + Path.Combine(strReleaseDir, strLicenseFileName));
                return;
            }

            // Create output directory
            Directory.CreateDirectory(strOutputDir);

            if (!File.Exists(Path.Combine(strOutputDir, strWixProjectFile)))
            {
                WriteWrappedText("Warning: Wix Project file was missing: " + Path.Combine(strOutputDir, strWixProjectFile));
                WriteWrappedText("Warning: you may need to adapt the solution configurations, so Setup project builds in x86 configuration");
                System.Console.WriteLine();
                string strWixProjFileContent = @"<Project Sdk=""WixToolset.Sdk/4.0.5"" ToolsVersion=""4.0"">" + Environment.NewLine +
                    "  <PropertyGroup>" +                                                                       Environment.NewLine +
                    "    <OutputType>Package</OutputType>" +                                                    Environment.NewLine +
                    "    <OutputName>"+Path.GetFileNameWithoutExtension(strWixProjectFile) +"</OutputName>" +   Environment.NewLine +
                    "    <PlatformTarget>x86</PlatformTarget>" +                                                Environment.NewLine +
                    "  </PropertyGroup>" +                                                                      Environment.NewLine +
                    "  <ItemGroup>" +                                                                           Environment.NewLine +
                    "    <PackageReference Include=\"WixToolset.UI.wixext\" Version=\"4.0.5\" />" +             Environment.NewLine +
                    "  </ItemGroup>" +                                                                          Environment.NewLine +
                    "  <ItemGroup>" +                                                                           Environment.NewLine +
                    "    <ProjectReference Include=\"..\\" +                                                    
                        strStartupProject.Substring(0,strStartupProject.LastIndexOf('.')) + "\\" +              
                        strStartupProject + "\" />" +                                                           Environment.NewLine +
                    "  </ItemGroup>" +                                                                          Environment.NewLine +
                    "</Project>";

                File.WriteAllText(Path.Combine(strOutputDir, strWixProjectFile), strWixProjFileContent, System.Text.Encoding.UTF8);
            }

            if (strWxsFile.Equals(""))
            {
                strWxsFile = strStartupProject.Substring(0, strStartupProject.LastIndexOf('.')) + ".wxs";
            }

            if (!File.Exists(Path.Combine(strOutputDir, strWxsFile)))
            {
                if (string.IsNullOrEmpty(strUpgradeCodeGuid))
                {
                    strUpgradeCodeGuid = Guid.NewGuid().ToString().ToUpper();
                };

                string strWxsFileContentInitial = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
                    "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\"\r\n"+
                    "     xmlns:ui=\"http://wixtoolset.org/schemas/v4/wxs/ui\">\r\n" +
                    "\t<Package Name=\""+strApplicationName+"\"\r\n" +
                    "\t\t   Language=\"1033\"\r\n" +
                    "\t\t   Version=\"1.0.0.0\"\r\n" +
                    "\t\t   Manufacturer=\""+ strCompanyName + "\"\r\n" +
                    "\t\t   UpgradeCode=\""+ strUpgradeCodeGuid +"\"\r\n" +
                    "\t\t   Scope=\"perMachine\">\r\n" +
                    "\t<MajorUpgrade DowngradeErrorMessage=\"A newer version of "+ SplitCamelCase(strApplicationName) + " has already been installed.\" />\r\n" +
                    "\t<MediaTemplate EmbedCab=\"yes\" />\r\n\r\n" +
                    "\t<Directory Id=\"TARGETDIR\" Name=\"SourceDir\">\r\n" +
                    "\t  <Directory Id=\"ProgramFilesFolder\">\r\n" +
                    "\t\t<Directory Id=\"PublisherFolder\" Name=\""+ strCompanyName + "\">\r\n" +
                    "\t\t  <Directory Id=\"INSTALLFOLDER\" Name=\"" + strApplicationName + "\" />\r\n" +
                    "\t\t</Directory>\r\n" +
                    "\t  </Directory>\r\n\r\n" +
                    "\t</Directory>\r\n\r\n" +
                    "\t<StandardDirectory Id=\"ProgramMenuFolder\">\r\n"+
                    "\t  <Directory Id=\"WindowsMenuFolder\" Name=\""+ strApplicationName + "\" />\r\n" +
                    "\t</StandardDirectory>\r\n"+
                    "\t<StandardDirectory Id=\"DesktopFolder\" />"+
                    "\t<WixVariable Id=\"WixUILicenseRtf\"\r\n" +
                    "\t  Value=\"$(var."+ strApplicationName + ".TargetDir)"+strLicenseFileName+"\" />\r\n" +
                    "\t<ui:WixUI Id=\"WixUI_InstallDir\"\r\n" +
                    "\t  InstallDirectory=\"INSTALLFOLDER\" />\r\n" +
                    "\t<DirectoryRef Id=\"INSTALLFOLDER\">\r\n" +
                    "\t</DirectoryRef>\r\n\r\n" +
                    "\t<DirectoryRef Id=\"WindowsMenuFolder\">\r\n" +
                    "\t  <Component Id=\"WindowsMenuShortcuts\" Guid=\"" + Guid.NewGuid().ToString().ToUpper()+"\">\r\n"+
                    "\t    <RemoveFolder Id=\"RemoveWindowsMenuFolder\" Directory=\"WindowsMenuFolder\" On=\"uninstall\" />\r\n"+
                    "\t    <RegistryValue Root=\"HKCU\" Key=\"Software\\"+strCompanyName+"\\"+strApplicationName+"\" Name=\"Installed\" Type=\"integer\" Value=\"1\" KeyPath=\"yes\" />\r\n"+
                    "\t  </Component>\r\n"+
                    "\t</DirectoryRef>\r\n"+
                    "\t<Feature Id=\""+MakeId(strApplicationName)+"\" Title=\"" + strApplicationName + "\" Level=\"1\">\r\n" +
                    "\t  <ComponentRef Id=\"WindowsMenuShortcuts\" />\r\n" +
                    "\t</Feature>\r\n" +
                    "\t</Package>\r\n" +
                    "</Wix>";

                File.WriteAllText(Path.Combine(strOutputDir, strWxsFile), strWxsFileContentInitial, System.Text.Encoding.UTF8);
            }


            // Generate .wxs file content
            GenerateWxsFileContent(Path.Combine(strOutputDir, strWxsFile), strReleaseDir, strApplicationName, strCompanyName, strStartupProject, bSkipPdb, strVersion);
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
                    {
                        if (bRightToLeft)
                            strCurrentLine = ReverseArabicAndHebrewParts(strCurrentLine);
                    }


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
            ref string rstrVersion,
            bool bWriteCopyright)
        {
            try
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
                        if (bWriteCopyright)
                            Console.WriteLine(strExeFile + " Copyright: " + oCopyrightAttribute.Copyright);
                    }

                }
                catch (BadImageFormatException oEx)
                {
                    // Native apphosts are not managed assemblies, but may still
                    // contain Windows version-resource metadata.
                    FileVersionInfo oFileVersion = FileVersionInfo.GetVersionInfo(strExeFile);

                    if (!string.IsNullOrWhiteSpace(oFileVersion.ProductName))
                        rstrAppName = oFileVersion.ProductName;

                    if (!string.IsNullOrWhiteSpace(oFileVersion.CompanyName))
                        rstrCompany = oFileVersion.CompanyName;

                    string strFileVersion = !string.IsNullOrWhiteSpace(oFileVersion.FileVersion)
                        ? oFileVersion.FileVersion:
                        oFileVersion.ProductVersion;

                    if (!string.IsNullOrWhiteSpace(strFileVersion))
                        rstrVersion = strFileVersion;

                    if (bWriteCopyright && !string.IsNullOrWhiteSpace(oFileVersion.LegalCopyright))
                        Console.WriteLine(strExeFile + " Copyright: " + oFileVersion.LegalCopyright);
                }

            }
            catch (Exception oEx)
            {
                Console.WriteLine("Error extracting metadata from "+
                    strExeFile + ": " + oEx.Message);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Collects all files in a directory
        /// </summary>
        /// <param name="strDirectory">Directory to scan</param>
        /// <param name="strSubdiectory">Current directory, relative to project output directory</param>
        /// <returns>All files in the </returns>
        //===================================================================================================
        static IEnumerable<string> CollectFiles(string strDirectory, string strSubdiectory)
        {
            foreach (FileInfo fi in new DirectoryInfo(strDirectory).GetFiles()) {
                yield return strSubdiectory != null ? strSubdiectory + "\\"+fi.Name : fi.Name;
            };

            foreach (DirectoryInfo di in new DirectoryInfo(strDirectory).GetDirectories())
            {
                foreach (string str in CollectFiles(di.FullName,
                    strSubdiectory != null ? strSubdiectory + "\\" + di.Name : di.Name))
                {
                    yield return str;
                }
            }
        }

        //===================================================================================================
        /// <summary>
        /// Formats a subpath to an ID
        /// </summary>
        /// <param name="strPath">Path relative to project folder</param>
        /// <returns>Stripped and modified path</returns>
        //===================================================================================================
        static string MakeId(string strPath)
        {
            return strPath.Replace("\\", "_").Replace(" ", "").Replace(".","_").Replace("-","");
        }

        //===================================================================================================
        /// <summary>
        /// Updates the content of the Wxs file
        /// </summary>
        /// <param name="strWxsPath">Path of the Wxs file</param>
        /// <param name="strReleaseDir">Release directory</param>
        /// <param name="strAppName">The name of the app to package</param>
        /// <param name="strCompanyName">The name of the company</param>
        /// <param name="strStartupProjectName">The name of the startup project file</param>
        /// <param name="bSkipPdb">Indication, if PDB files should not be packaged</param>
        /// <param name="strVersion">The version to set in the package</param>
        //===================================================================================================
        static void GenerateWxsFileContent(
            string strWxsPath, 
            string strReleaseDir, 
            string strAppName, 
            string strCompanyName, 
            string strStartupProjectName, 
            bool bSkipPdb, 
            string strVersion
            )
        {
            var oNameSpaces = new XmlNamespaceManager(new NameTable());
            oNameSpaces.AddNamespace("w", "http://wixtoolset.org/schemas/v4/wxs");

            XDocument oWxs = XDocument.Load(strWxsPath);
            string strOriginalDocument = oWxs.Root.ToString();

            oWxs.XPathSelectElement("/w:Wix/w:Package", oNameSpaces).SetAttributeValue("Version", strVersion);

            foreach (string strFile in CollectFiles(strReleaseDir, null))
            {
                // If PDBs shall be skipped, then do this
                if (strFile.EndsWith(".pdb",StringComparison.InvariantCultureIgnoreCase) && bSkipPdb)
                {
                    continue;
                };

                string strId = MakeId(strFile).Replace(strAppName+"_resources_dll","resources_dll");
                XElement oElement = oWxs.XPathSelectElement("/w:Wix/w:Package/w:DirectoryRef[@Id='INSTALLFOLDER']/w:Component/w:File[@Source='$(var."+ strAppName + ".TargetDir)" + strFile + "']", oNameSpaces);
                if (oElement != null)
                {
                    if (oElement.Attribute("Id") == null)
                        oElement.SetAttributeValue("Id", strId);
                    //oElement.SetAttributeValue("Source", "$(var.CreateMsiFromFolder.TargetDir)" + strFile);
                } else
                {
                    string strDir = strFile.Contains('\\')?strFile.Substring(0, strFile.LastIndexOf("\\")):"";
                    string strDirId = MakeId(string.IsNullOrEmpty(strDir)?"_":strDir);

                    XElement oComponentElement = oWxs.XPathSelectElement("/w:Wix/w:Package/w:DirectoryRef[@Id='INSTALLFOLDER']/w:Component[@Id='"+strDirId+"']", oNameSpaces);
                    if (oComponentElement == null)
                    {
                        XElement oDirElement = oWxs.XPathSelectElement("/w:Wix/w:Package/w:DirectoryRef[@Id='INSTALLFOLDER']", oNameSpaces);
                        if (oDirElement == null)
                        {
                            System.Console.WriteLine("'Couldn't find /Wix/Package/DirectoryRef[@Id='INSTALLFOLDER']", oNameSpaces);
                            return;
                        }

                        if (!string.IsNullOrEmpty(strDir))
                        {
                            oDirElement.Add(
                                oComponentElement = new XElement(oDirElement.Name.Namespace + "Component",
                                    new XAttribute("Id", strDirId),
                                    new XAttribute("Guid", Guid.NewGuid().ToString().ToUpper()),    
                                    new XAttribute("Subdirectory", strDir)
                                ));
                        } else
                        {
                            oDirElement.Add(
                                oComponentElement = new XElement(oDirElement.Name.Namespace + "Component",
                                    new XAttribute("Id", strDirId),
                                    new XAttribute("Guid", Guid.NewGuid().ToString().ToUpper())
                                ));
                        }

                        // Add the component also to feature
                        XElement oFeatureElement = oWxs.XPathSelectElement("/w:Wix/w:Package/w:Feature/w:ComponentRef[Id='"+strDirId+"']",oNameSpaces);
                        if (oFeatureElement == null)
                        {
                            oFeatureElement = oWxs.XPathSelectElement("/w:Wix/w:Package/w:Feature",oNameSpaces);
                            oFeatureElement.Add(
                                new XElement(oFeatureElement.Name.Namespace + "ComponentRef",
                                    new XAttribute("Id", strDirId)));
                        }
                    }

                    if (strFile.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase))
                    {
                        XElement oFileElement =
                        new XElement(oComponentElement.Name.Namespace + "File",
                            new XAttribute("Id", strId),
                            new XAttribute("Source", "$(var." + strStartupProjectName.Substring(0, strStartupProjectName.LastIndexOf('.')) + ".TargetDir)" + strFile));


                        XElement oComponentElement2 = oWxs.XPathSelectElement("/w:Wix/w:Package/w:DirectoryRef[@Id='WindowsMenuFolder']/w:Component[@Id='WindowsMenuShortcuts']", oNameSpaces);
                        if (oComponentElement2 == null) {
                            System.Console.WriteLine("Error: couldn't find /Wix/Package/DirectoryRef[@Id='WindowsMenuFolder']/Component[@Id='WindowsMenuShortcuts']");
                        } else
                        {
                            oComponentElement2.Add(
                                new XElement(oComponentElement2.Name.Namespace + "Shortcut",
                                    new XAttribute("Id", strId + "_pfx"),
                                    new XAttribute("Directory", "WindowsMenuFolder"),
                                    new XAttribute("Name", SplitCamelCase(strFile.Substring(strFile.LastIndexOf('\\') + 1).Replace(".exe", ""))),
                                    new XAttribute("Target", "[INSTALLFOLDER]" + strFile),
                                    new XAttribute("WorkingDirectory", "INSTALLFOLDER")
                                ));
                            oComponentElement2.Add(
                                new XElement(oComponentElement2.Name.Namespace + "Shortcut",
                                    new XAttribute("Id", strId + "_2_pfx"),
                                    new XAttribute("Directory", "DesktopFolder"),
                                    new XAttribute("Name", SplitCamelCase(strFile.Substring(strFile.LastIndexOf('\\') + 1).Replace(".exe", ""))),
                                    new XAttribute("Target", "[INSTALLFOLDER]" + strFile),
                                    new XAttribute("WorkingDirectory", "INSTALLFOLDER")
                                ));

                        }

                        if (oComponentElement.XPathSelectElement("w:File[@KeyPath='yes']", oNameSpaces) == null)
                        {

                            oFileElement.Add(new XAttribute("KeyPath", "yes"));

                            /*
                            if (oComponentElement.XPathSelectElement("RemoveFolder", oNameSpaces) == null)
                            {
                                oComponentElement.Add(
                                    new XElement(oComponentElement.Name.Namespace + "RemoveFolder",
                                        new XAttribute("Id", strDirId + "_Remove"),
                                        new XAttribute("Directory", "WindowsMenuFolder"),
                                        new XAttribute("On", "uninstall")
                                    ));
                            }
                            */
                        }
                        oComponentElement.Add(oFileElement);
                    }
                    else
                    {
                        oComponentElement.Add(
                            new XElement(oComponentElement.Name.Namespace + "File",
                                new XAttribute("Id", strId),
                                new XAttribute("Source", "$(var." + strStartupProjectName.Substring(0, strStartupProjectName.LastIndexOf('.')) + ".TargetDir)" + strFile)
                            ));
                    }
                }
            }

            string strModifiedDocument = oWxs.Root.ToString();
            if (!strModifiedDocument.Equals(strOriginalDocument))
            {
                File.WriteAllText(strWxsPath, strModifiedDocument, System.Text.Encoding.UTF8);
            }
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
            string[] astrParts = strCamelCaseInput.Split('_');

            for (int i = 0; i < astrParts.Length; i++)
            {
                if (astrParts[i].Length == 0)
                {
                    continue;
                }

                bool bAllUppercase = true;
                foreach (char c in astrParts[i])
                {
                    if (!char.IsUpper(c))
                    {
                        bAllUppercase = false;
                        break;
                    }
                }

                if (bAllUppercase)
                {
                    astrParts[i] = astrParts[i][0] +
                               astrParts[i].Substring(1).ToLower();
                }
                else
                {
                    astrParts[i] = char.ToUpper(astrParts[i][0]) + astrParts[i].Substring(1);
                }
            }

            return Regex.Replace(string.Join(" ", astrParts), "([a-z])([A-Z])", "$1 $2");
        }

        //===================================================================================================
        /// <summary>
        /// Executes a command line command with arguments
        /// </summary>
        /// <param name="strApp">App to start</param>
        /// <param name="strParameters">the command line arguments</param>
        //===================================================================================================
        static void Execute(string strApp, string strParameters)
        {
            System.Console.WriteLine(strApp + " " + strParameters);

            ProcessStartInfo oProcessStartInfo = new ProcessStartInfo();
            oProcessStartInfo.FileName = strApp; // Replace with your target executable
            oProcessStartInfo.Arguments = strParameters; // Add arguments if needed
            oProcessStartInfo.UseShellExecute = false;
            oProcessStartInfo.RedirectStandardOutput = true;
            oProcessStartInfo.CreateNoWindow = true;

            using (Process process = new Process())
            {
                process.StartInfo = oProcessStartInfo;
                process.Start();

                // Read output line by line and write to your console
                while (!process.StandardOutput.EndOfStream)
                {
                    string line = process.StandardOutput.ReadLine();
                    Console.WriteLine(line);
                }

                process.WaitForExit();
            }
        }

        static void CompileAndLinkWiX(
            string strWxsFilePath, 
            string strOutputDir, 
            string strApplicationName
            )
        {
            try
            {
                string strWxsFileName = Path.GetFileNameWithoutExtension(strWxsFilePath);
                string strWixObjFilePath = Path.Combine(strOutputDir, strWxsFileName + ".wixobj");
                string strMsiFilePath = Path.Combine(strOutputDir, strApplicationName + ".msi");

                string strEnWxlFile = Path.Combine(strOutputDir, "en-us.wxl");
                string strDeWxlFile = Path.Combine(strOutputDir, "de-de.wxl");

                Console.WriteLine("Compiling with WiX candle.exe...");
                Execute("candle.exe", "-o \"" + strWixObjFilePath + "\" \"" + strWxsFilePath + "\"");

                Console.WriteLine("Linking with WiX light.exe...");
                Execute("light.exe", "-out \"" + strMsiFilePath + "\" \"" + strWixObjFilePath + "\" -loc \"" + strEnWxlFile + "\" -loc \"" + strDeWxlFile + "\"");

                Console.WriteLine("Installer created at: " + strMsiFilePath);
            }
            catch (Exception oEx)
            {
                Console.Error.WriteLine(oEx);
            }
        }
    }

}
