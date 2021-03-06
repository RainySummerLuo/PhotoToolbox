﻿using System;
using System.IO;
using System.Security.Cryptography;
using static MediaRenamer.MetadataQuery;
using static MediaRenamer.Program;

namespace MediaRenamer {
    public static class FileHandle {
        internal static bool FileProcess(FileSystemInfo file) {
            try {
                string strDt;
                var fileExt = file.Extension.ToLower();
                if (PicExt.Contains(fileExt)) {
                    var dictResult = MetaQuery(file, true);
                    if (dictResult == null) {
                        Console.WriteLine("[-Error-] Pic MetaQuery returns NULL result: " + file.FullName + ".");
                        return false;
                    } else if (dictResult.ContainsKey("error")) {
                        Console.WriteLine("[-Error-] Pic MetaQuery error: " + dictResult["error"] + ".");
                        return false;
                    } else {
                        strDt = dictResult["datetime"];
                        Rename(file, strDt);
                    }
                } else if (VidExt.Contains(fileExt)) {
                    var dictResult = MetaQuery(file, false);
                    if (dictResult == null) {
                        Console.WriteLine("[-Error-] Vid MetaQuery returns NULL result: " + file.FullName + ".");
                        return false;
                    } else if (dictResult.ContainsKey("error")) {
                        Console.WriteLine("[-Error-] Pic MetaQuery error: " + dictResult["error"] + ".");
                        return false;
                    } else {
                        strDt = dictResult["datetime"];
                        Rename(file, strDt);
                    }
                } else {
                    var dictResult = MetaQuery(file, true);
                    // try as pic
                    if (dictResult == null || dictResult.ContainsKey("error")) {
                        dictResult = MetaQuery(file, false);
                        // or else try as vid
                    }

                    if (dictResult == null) {
                        Console.WriteLine("[-Error-] MetaQuery returns NULL result: " + file.FullName + ".");
                        return false;
                    } else if (dictResult.ContainsKey("error")) {
                        Console.WriteLine("[-Error-] MetaQuery error: " + dictResult["error"] + ".");
                        return false;
                    } else {
                        strDt = dictResult["datetime"];
                        Rename(file, strDt);
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine("[-Error-] FileProcess error: " + ex.Message + ".");
                return false;
            }

            return true;
        }

        private static void Rename(FileSystemInfo file, string strDt) {
            var strOriginName = file.FullName;
            var strPath = file.FullName.Replace(file.Name, "");
            var strFullName = Path.Combine(strPath, strDt);
            var strExt = file.Extension.ToLower();
            var intDupInx = 0;
            var strDupName = strFullName + strExt;
            if (File.Exists(strDupName) && FileCompare(strDupName, strOriginName)) {
                Console.WriteLine("[Skipped] " + file.FullName);
                return;
            }

            while (File.Exists(strDupName)) {
                intDupInx++;
                var strDupInx = "_" + intDupInx;
                strDupName = strFullName + strDupInx + strExt;
            }

            var strNewName = strDupName;
            try {
                var fileInfo = new FileInfo(strOriginName);
                fileInfo.MoveTo(strNewName);
                Console.WriteLine("[-Moved-] " + " --> " + strNewName);
            } catch (Exception) {
                Console.WriteLine("[-Error-] " + " --> Error rename");
            }
        }

        private static bool FileCompare(string filePath1, string filePath2) {
            if (filePath1 == filePath2) {
                return true;
            }
            using (var hash = HashAlgorithm.Create()) {
                using (FileStream file1 = new FileStream(filePath1, FileMode.Open),
                                  file2 = new FileStream(filePath2, FileMode.Open)) {
                    var hashByte1 = hash.ComputeHash(file1);
                    var hashByte2 = hash.ComputeHash(file2);
                    var fileByte1 = BitConverter.ToString(hashByte1);
                    var fileByte2 = BitConverter.ToString(hashByte2);
                    return fileByte1 == fileByte2;
                }
            }
        }
    }
}