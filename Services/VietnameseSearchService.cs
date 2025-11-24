using System.Text;
using System.Text.RegularExpressions;

namespace webxemphim.Services
{
    /// <summary>
    /// Service để xử lý tìm kiếm tiếng Việt với khả năng tìm kiếm không dấu
    /// </summary>
    public class VietnameseSearchService
    {
        private static readonly Dictionary<char, string> VietnameseDiacriticsMap = new()
        {
            {'à', "a"}, {'á', "a"}, {'ạ', "a"}, {'ả', "a"}, {'ã', "a"}, {'â', "a"}, {'ầ', "a"}, {'ấ', "a"}, {'ậ', "a"}, {'ẩ', "a"}, {'ẫ', "a"},
            {'ă', "a"}, {'ằ', "a"}, {'ắ', "a"}, {'ặ', "a"}, {'ẳ', "a"}, {'ẵ', "a"},
            {'è', "e"}, {'é', "e"}, {'ẹ', "e"}, {'ẻ', "e"}, {'ẽ', "e"}, {'ê', "e"}, {'ề', "e"}, {'ế', "e"}, {'ệ', "e"}, {'ể', "e"}, {'ễ', "e"},
            {'ì', "i"}, {'í', "i"}, {'ị', "i"}, {'ỉ', "i"}, {'ĩ', "i"},
            {'ò', "o"}, {'ó', "o"}, {'ọ', "o"}, {'ỏ', "o"}, {'õ', "o"}, {'ô', "o"}, {'ồ', "o"}, {'ố', "o"}, {'ộ', "o"}, {'ổ', "o"}, {'ỗ', "o"},
            {'ơ', "o"}, {'ờ', "o"}, {'ớ', "o"}, {'ợ', "o"}, {'ở', "o"}, {'ỡ', "o"},
            {'ù', "u"}, {'ú', "u"}, {'ụ', "u"}, {'ủ', "u"}, {'ũ', "u"}, {'ư', "u"}, {'ừ', "u"}, {'ứ', "u"}, {'ự', "u"}, {'ử', "u"}, {'ữ', "u"},
            {'ỳ', "y"}, {'ý', "y"}, {'ỵ', "y"}, {'ỷ', "y"}, {'ỹ', "y"},
            {'đ', "d"},
            {'À', "A"}, {'Á', "A"}, {'Ạ', "A"}, {'Ả', "A"}, {'Ã', "A"}, {'Â', "A"}, {'Ầ', "A"}, {'Ấ', "A"}, {'Ậ', "A"}, {'Ẩ', "A"}, {'Ẫ', "A"},
            {'Ă', "A"}, {'Ằ', "A"}, {'Ắ', "A"}, {'Ặ', "A"}, {'Ẳ', "A"}, {'Ẵ', "A"},
            {'È', "E"}, {'É', "E"}, {'Ẹ', "E"}, {'Ẻ', "E"}, {'Ẽ', "E"}, {'Ê', "E"}, {'Ề', "E"}, {'Ế', "E"}, {'Ệ', "E"}, {'Ể', "E"}, {'Ễ', "E"},
            {'Ì', "I"}, {'Í', "I"}, {'Ị', "I"}, {'Ỉ', "I"}, {'Ĩ', "I"},
            {'Ò', "O"}, {'Ó', "O"}, {'Ọ', "O"}, {'Ỏ', "O"}, {'Õ', "O"}, {'Ô', "O"}, {'Ồ', "O"}, {'Ố', "O"}, {'Ộ', "O"}, {'Ổ', "O"}, {'Ỗ', "O"},
            {'Ơ', "O"}, {'Ờ', "O"}, {'Ớ', "O"}, {'Ợ', "O"}, {'Ở', "O"}, {'Ỡ', "O"},
            {'Ù', "U"}, {'Ú', "U"}, {'Ụ', "U"}, {'Ủ', "U"}, {'Ũ', "U"}, {'Ư', "U"}, {'Ừ', "U"}, {'Ứ', "U"}, {'Ự', "U"}, {'Ử', "U"}, {'Ữ', "U"},
            {'Ỳ', "Y"}, {'Ý', "Y"}, {'Ỵ', "Y"}, {'Ỷ', "Y"}, {'Ỹ', "Y"},
            {'Đ', "D"}
        };

        /// <summary>
        /// Chuyển đổi chuỗi tiếng Việt có dấu thành không dấu
        /// </summary>
        public static string RemoveDiacritics(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            var sb = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                if (VietnameseDiacriticsMap.TryGetValue(c, out var replacement))
                {
                    sb.Append(replacement);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Tạo pattern tìm kiếm hỗ trợ cả có dấu và không dấu
        /// </summary>
        public static string CreateSearchPattern(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return string.Empty;

            var normalized = RemoveDiacritics(searchTerm).ToLower();
            var pattern = new StringBuilder();
            
            foreach (var c in normalized)
            {
                // Tạo pattern cho từng ký tự, hỗ trợ cả có dấu và không dấu
                switch (c)
                {
                    case 'a':
                        pattern.Append("[aàáạảãâầấậẩẫăằắặẳẵAÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴ]");
                        break;
                    case 'e':
                        pattern.Append("[eèéẹẻẽêềếệểễEÈÉẸẺẼÊỀẾỆỂỄ]");
                        break;
                    case 'i':
                        pattern.Append("[iìíịỉĩIÌÍỊỈĨ]");
                        break;
                    case 'o':
                        pattern.Append("[oòóọỏõôồốộổỗơờớợởỡOÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠ]");
                        break;
                    case 'u':
                        pattern.Append("[uùúụủũưừứựửữUÙÚỤỦŨƯỪỨỰỬỮ]");
                        break;
                    case 'y':
                        pattern.Append("[yỳýỵỷỹYỲÝỴỶỸ]");
                        break;
                    case 'd':
                        pattern.Append("[dđDĐ]");
                        break;
                    default:
                        pattern.Append(Regex.Escape(c.ToString()));
                        break;
                }
            }
            
            return pattern.ToString();
        }

        /// <summary>
        /// Kiểm tra xem text có chứa searchTerm không (hỗ trợ cả có dấu và không dấu)
        /// </summary>
        public static bool ContainsIgnoreDiacritics(string text, string searchTerm, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(searchTerm))
                return false;

            // Thử tìm kiếm trực tiếp trước (nhanh hơn)
            if (text.Contains(searchTerm, comparison))
                return true;

            // Nếu không tìm thấy, thử tìm kiếm không dấu
            var textNormalized = RemoveDiacritics(text);
            var searchNormalized = RemoveDiacritics(searchTerm);
            return textNormalized.Contains(searchNormalized, comparison);
        }
    }
}

