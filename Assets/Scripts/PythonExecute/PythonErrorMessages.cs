using System;
using System.Text.RegularExpressions;

/// <summary>
/// サーバーから届く Python のエラー文を、初めてコードを書く人にも分かる日本語に言い換える
/// </summary>
public static class PythonErrorMessages
{
    // 構文エラーは行番号が "...(<unknown>, line 4)" のようにメッセージの中にしか入っていない
    private static readonly Regex LineInMessage = new Regex(@"line (\d+)\)\s*$");

    // 打ち間違いの候補として示す名前（ゲームの関数と、コードの先頭に用意される変数）
    private static readonly string[] KnownNames =
    {
        "Attack", "Guard", "Cast", "MoveTo", "MoveForward", "MoveBackward", "Swap",
        "print", "range", "enemy", "enemies", "count", "numbers",
    };

    /// <summary>
    /// メッセージの中に書かれた行番号を返す。書かれていなければ 0
    /// </summary>
    public static int LineInSyntaxError(string message)
    {
        Match match = LineInMessage.Match(message);
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    public static string ToJapanese(string message)
    {
        Match m;

        // 吹き出しが小さいので、どれも短く言い切る
        if ((m = Regex.Match(message, @"'(.)' was never closed")).Success)
            return $"「{m.Groups[1].Value}」が閉じていません。「{ClosingOf(m.Groups[1].Value)}」を付けましょう";
        if ((m = Regex.Match(message, @"unmatched '(.)'")).Success)
            return $"「{m.Groups[1].Value}」が多すぎます";
        if (message.Contains("unterminated string literal") || message.Contains("EOL while scanning"))
            return "「\"」が閉じていません";
        if (message.Contains("expected an indented block"))
            return "この行の先頭に半角スペース4つを入れましょう";
        if (message.Contains("unexpected indent"))
            return "行の先頭の空白をなくしましょう";
        if (message.Contains("unindent does not match") || message.Contains("inconsistent use of tabs"))
            return "字下げの幅をそろえましょう（半角スペース4つずつ）";
        if (message.Contains("expected ':'"))
            return "行の最後に「:」を付けましょう";
        if (message.Contains("invalid character") || message.Contains("invalid non-printable character"))
            return "全角の文字があります。半角で書きましょう";
        if ((m = Regex.Match(message, @"name '(\w+)' is not defined")).Success)
            return UnknownName(m.Groups[1].Value);
        if (message.Contains("required positional argument"))
            return "関数に渡す値が足りません";
        if (Regex.IsMatch(message, @"takes \d+ positional arguments? but \d+ (were|was) given"))
            return "関数に渡す値が多すぎます";
        if (message.StartsWith("Syntax error"))
            return "書き方に誤りがあります（かっこ・「:」・つづり）";

        // 言い換えられないものは、頭の決まり文句だけ外してそのまま見せる
        return Regex.Replace(message, @"^(Runtime error|Error): ", "");
    }

    private static string UnknownName(string name)
    {
        string suggestion = Array.Find(KnownNames, known => string.Equals(known, name, StringComparison.OrdinalIgnoreCase));
        if (suggestion != null)
            return $"「{name}」ではなく「{suggestion}」です（大文字・小文字は区別されます）";
        return $"「{name}」という名前はありません";
    }

    private static string ClosingOf(string opening)
    {
        return opening switch { "(" => ")", "[" => "]", "{" => "}", _ => opening };
    }
}
