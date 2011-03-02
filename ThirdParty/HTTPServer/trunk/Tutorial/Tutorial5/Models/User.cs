using Fadd.Validation;

namespace Tutorial.Tutorial5.Models
{
    public class User
    {
        private string _userName;
        private string _firstName;
        private string _lastName;

        [ValidateBetween(4,20)]
        [ValidateRequired]
        public string UserName
        {
            get { return _userName; }
            set { _userName = value; }
        }

        [ValidateBetween(2, 20)]
        [ValidateLettersAndDigits(" .,")]
        public string FirstName
        {
            get { return _firstName; }
            set { _firstName = value; }
        }

        [ValidateBetween(2, 20)]
        [ValidateLettersAndDigits(" .,")]
        public string LastName
        {
            get { return _lastName; }
            set { _lastName = value; }
        }
    }
}
