using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace zformat
{
    class Program
    {
        struct type_section_info
        {
            public string name;
            public int start_address;
            public int size;
        }

        static void Main(string[] args)
        {
            try
            {
                var input_info = Console.In.ReadToEnd();

                // разбиваем на строки
                var reg_string = new Regex("[a-z:0-9 ,.()_]+[\n]*", RegexOptions.Compiled);
                var input_string = reg_string.Matches(input_info);

                // выделяем информацию о каждой секции
                type_section_info[] section_info = new type_section_info[(input_string.Count - 1) / 2];
                var reg_name = new Regex("(?<=[.])[a-z_]*", RegexOptions.Compiled);
                var reg_size = new Regex("(?<=[(])[0][a-z0-9]*", RegexOptions.Compiled);
                var reg_start_address = new Regex("(?<=address )[a-fx0-9]*", RegexOptions.Compiled);
                for (var i = 0; i < section_info.Length; i++)
                {
                    section_info[i].name = reg_name.Matches(input_string[i * 2 + 1].ToString())[0].ToString();
                    section_info[i].size = Convert.ToInt32(reg_size.Matches(input_string[i * 2 + 2].ToString())[0].ToString(), 16);
                    section_info[i].start_address = Convert.ToInt32(reg_start_address.Matches(input_string[i * 2 + 1].ToString())[0].ToString(), 16);
                }

                // получаем размер секций
                var ram_size = 0;
                var flash_size = 0;
                var eeprom_size = 0;
                var eeprom_start_address = 0;
                var reg_ram_size = new Regex("(?<=__endmem=)[a-fx0-9]*", RegexOptions.Compiled);
                var reg_flash_size = new Regex("(?<=seg .const -b 0x[a-fx0-9]* -m )[a-fx0-9]*", RegexOptions.Compiled);
                var reg_eeprom_size = new Regex("(?<=eeprom -b 0x[a-fx0-9]* -m )[a-fx0-9]*", RegexOptions.Compiled);
                var reg_eeprom_start_address = new Regex("(?<=eeprom -b )[a-fx0-9]*", RegexOptions.Compiled);
                if (args.Length != 0)
                {
                    if (File.Exists(args[0]))
                    {
                        var input_section_size = File.ReadAllText(args[0]);

                        ram_size = Convert.ToInt32(reg_ram_size.Matches(input_section_size.ToString())[0].ToString(), 16);
                        flash_size = Convert.ToInt32(reg_flash_size.Matches(input_section_size.ToString())[0].ToString(), 16);
                        eeprom_size = Convert.ToInt32(reg_eeprom_size.Matches(input_section_size.ToString())[0].ToString(), 16);
                        eeprom_start_address = Convert.ToInt32(reg_eeprom_start_address.Matches(input_section_size.ToString())[0].ToString(), 16);
                    }
                }

                // группируем информацию о секциях
                var ram_use = 0;
                var flash_use = 0x80;
                var eeprom_use = 0;
                for (var i = 0; i < section_info.Length; i++)
                {
                    if (section_info[i].size != 0)
                    {
                        if (section_info[i].name != "info")
                        {
                            if (section_info[i].name != "debug")
                            {
                                if (section_info[i].start_address >= 0x8000)
                                {
                                    flash_use += section_info[i].size;
                                }
                                else if (section_info[i].start_address >= eeprom_start_address)
                                {
                                    eeprom_use += section_info[i].size;
                                }
                                else
                                {
                                    ram_use += section_info[i].size;
                                }
                            }
                        }
                    }
                }

                //
                flash_use -= 128;

                //
                Console.WriteLine("FLASH: " + "\t[{0,4:0.0}%]\t{1,8:0} bytes", ((float)flash_use * 100.0) / flash_size, flash_use);
                Console.WriteLine("RAM:   " + "\t[{0,4:0.0}%]\t{1,8:0} bytes", ((float)ram_use * 100.0) / ram_size, ram_use);
                Console.WriteLine("EEPROM:" + "\t[{0,4:0.0}%]\t{1,8:0} bytes", ((float)eeprom_use * 100.0) / eeprom_size, eeprom_use);
            }
            catch (Exception error)
            {
                Console.WriteLine(error.Message);
            }
        }
    }
}
