using System.Text;

public static class UserNameValidator
{
    public static bool CheckName(string name, out string errorMessage)
    {
        if (string.IsNullOrEmpty(name))
        {
            errorMessage = "Имя не должно быть пустым";
            return false;
        }
        if (Encoding.Default.GetBytes(name).Length > 20)
        {
            errorMessage = "Имя не должно быть длинне 20 символов";
            return false;
        }
        errorMessage = null;
        return true;
    }
}
