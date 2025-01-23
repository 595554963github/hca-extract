using System;
using System.IO;
using System.Linq;

namespace HCAExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("请输入要处理的文件夹路径: ");
            string? inputPath = Console.ReadLine();

            // 将 inputPath 赋值给 dirPath，并且修正检查目录是否存在的逻辑
            string dirPath = inputPath ?? "";
            if (!Directory.Exists(dirPath))
            {
                Console.WriteLine($"错误: {dirPath} 不是一个有效的目录。");
                return;
            }

            byte[] startSeq1 = { 0x48, 0x43, 0x41, 0x00 };
            byte[] startSeq2 = { 0xC8, 0xC3, 0xC1, 0x00, 0x03, 0x00, 0x00, 0x60 };
            byte[] hcaBlockMarker = { 0x66, 0x6D, 0x74 };

            foreach (string file in Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories))
            {
                Console.WriteLine($"Processing file: {file}");
                byte[] content = File.ReadAllBytes(file);

                bool found = false;
                int start_index = 0;

                while (!found)
                {
                    start_index = FindSequence(content, startSeq1, start_index);
                    if (start_index == -1)
                    {
                        break;
                    }

                    int next_start_index = FindSequence(content, startSeq1, start_index + startSeq1.Length);
                    int end_index = next_start_index == -1 ? content.Length : next_start_index;

                    byte[] extracted = content.Skip(start_index).Take(end_index - start_index).ToArray();
                    if (ContainsSequence(extracted, hcaBlockMarker))
                    {
                        ExtractHca(file, dirPath, startSeq1, hcaBlockMarker);
                        found = true;
                    }

                    start_index += startSeq1.Length;
                }

                if (!found)
                {
                    ExtractHca(file, dirPath, startSeq2);
                }
            }
        }

        static void ExtractHca(string filePath, string directoryPath, byte[] startSeq, byte[]? blockMarker = null)
        {
            byte[] content = File.ReadAllBytes(filePath);
            int start_index = 0;
            int count = 0;

            while (true)
            {
                start_index = FindSequence(content, startSeq, start_index);
                if (start_index == -1)
                {
                    break;
                }

                int next_start_index = FindSequence(content, startSeq, start_index + startSeq.Length);
                int end_index = next_start_index == -1 ? content.Length : next_start_index;

                byte[] extracted = content.Skip(start_index).Take(end_index - start_index).ToArray();

                if (blockMarker == null || ContainsSequence(extracted, blockMarker))
                {
                    string fileExtension = ".hca";
                    string baseName = Path.GetFileNameWithoutExtension(filePath);
                    string newFileName = $"{baseName}_{count}{fileExtension}";

                    if (startSeq.SequenceEqual(new byte[] { 0xC8, 0xC3, 0xC1, 0x00, 0x03, 0x00, 0x00, 0x60 }))
                    {
                        newFileName = $"{baseName}_{count}_enc{fileExtension}";
                    }

                    string newFilePath = Path.Combine(directoryPath, newFileName);
                    File.WriteAllBytes(newFilePath, extracted);
                    Console.WriteLine($"Extracted and saved {newFileName}");
                    count++;
                }

                start_index += startSeq.Length;
            }
        }

        static int FindSequence(byte[] content, byte[] sequence, int startIndex)
        {
            if (startIndex < 0 || startIndex >= content.Length)
            {
                return -1;
            }

            for (int i = startIndex; i <= content.Length - sequence.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < sequence.Length; j++)
                {
                    if (content[i + j] != sequence[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    return i;
                }
            }
            return -1;
        }

        static bool ContainsSequence(byte[] content, byte[] sequence)
        {
            for (int i = 0; i <= content.Length - sequence.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < sequence.Length; j++)
                {
                    if (content[i + j] != sequence[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    return true;
                }
            }
            return false;
        }
    }
}