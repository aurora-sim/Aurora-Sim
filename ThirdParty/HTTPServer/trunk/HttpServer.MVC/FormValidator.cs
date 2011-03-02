using System;
using System.Collections.Specialized;
using Fadd;
using Fadd.Globalization;

namespace HttpServer.MVC.Helpers
{
    /// <summary>
    /// Validator is used to validate all input items in a form.
    /// </summary>
    public class FormValidator : Validator
    {
        private IHttpInput _form;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormValidator"/> class.
        /// </summary>
        /// <param name="errors">collection to be filled with errors</param>
        public FormValidator(NameValueCollection errors) : base(errors)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormValidator"/> class.
        /// </summary>
        /// <param name="errors">collection to be filled with errors</param>
        /// <param name="modelLanguage">Translation used to translate the "name" parameters in all validation methods.</param>
        public FormValidator(NameValueCollection errors, ILanguageNode modelLanguage)
            : base(errors, modelLanguage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormValidator"/> class.
        /// </summary>
        /// <param name="modelLanguage">Translation used to translate the "name" parameters in all validation methods.</param>
        public FormValidator(ILanguageNode modelLanguage)
            : base(modelLanguage)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="form">form that validation should be made on.</param>
        public FormValidator(IHttpInput form)
            : this(form, new NameValueCollection())
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="errors">collection that all validation errors are added to.</param>
        /// <param name="form">form that validation should be made on.</param>
        public FormValidator(IHttpInput form, NameValueCollection errors) : base(errors)
        {
            if (form == null)
                throw new ArgumentNullException("form");

            _form = form;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="errors">collection that all validation errors are added to.</param>
        /// <param name="form">form that validation should be made on.</param>
        /// <param name="modelLanguage">Language category used to translate field names.</param>
        public FormValidator(IHttpInput form, NameValueCollection errors, ILanguageNode modelLanguage)
            : base(errors, modelLanguage)
        {
            if (form == null)
                throw new ArgumentNullException("form");

            _form = form;
        }

        /// <summary>
        /// Switch to a new http input.
        /// </summary>
        /// <param name="form">form to use validation for now</param>
        public void SetForm(IHttpInput form)
        {
            if (form == null)
                throw new ArgumentNullException("form");
            _form = form;
        }

        /// <summary>
        /// Switch to a new http input.
        /// </summary>
        /// <param name="form">form to use validation for now</param>
        /// <param name="modelLanguage">language for the validation</param>
        public void SetForm(IHttpInput form, ILanguageNode modelLanguage)
        {
            if (form == null)
                throw new ArgumentNullException("form");
            if (modelLanguage == null)
                throw new ArgumentNullException("modelLanguage");
            _form = form;
            _modelLang = modelLanguage;
        }


        /// <summary>
        /// Check if a value is digits only
        /// </summary>
        /// <param name="name">Field name.</param>
        /// <param name="required">true if field is required (may not be empty)</param>
        /// <returns>string if validated, otherwise string.Empty</returns>
        public string Digits(string name, bool required)
        {
            return Digits(name, _form[name].Value, required);
        }

        /// <summary>
        /// Check if a value is digits only
        /// </summary>
        /// <param name="name">Field name.</param>
        /// <param name="extraAllowedCharacters">extra characters that is allowed.</param>
        /// <param name="required">true if field is required (may not be empty)</param>
        /// <returns>string if validated, otherwise string.Empty</returns>
        public string Digits(string name, bool required, string extraAllowedCharacters)
        {
            return Digits(name, _form[name].Value, required, extraAllowedCharacters);
        }
        /// <summary>
        /// Check whether the specified form item is an integer.
        /// </summary>
        /// <param name="name">Form parameter to validate</param>
        /// <returns>value if parameter is an int; 0 if not.</returns>
        public int Integer(string name)
        {
            return Integer(name, _form[name].Value, false);
        }

        /// <summary>
        /// Check whether the specified form item is an integer.
        /// </summary>
        /// <param name="name">Form parameter to validate</param>
        /// <param name="required">Paramater is required (adds an error if it's not specified)</param>
        /// <returns>value if parameter is an int; 0 if not.</returns>
        public int Integer(string name, bool required)
        {
            return Integer(name, _form[name].Value, required);
        }

        /// <summary>
        /// Check whether the specified value is a double.
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="required">Paramater is required (adds an error if it's not specified)</param>
        /// <returns>value if parameter is a double; 0 if not.</returns>
        public double Double(string name, bool required)
        {
            return Double(name, _form[name].Value, required, FieldDouble);
        }


        /// <summary>
        /// Check whether the specified value is a currency amount.
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="required">Paramater is required (adds an error if it's not specified)</param>
        /// <returns>value if parameter is a currency amount; 0 if not.</returns>
        public double Currency(string name, bool required)
        {
            return Double(name, _form[name].Value, required, FieldCurrency);
        }

		/// <summary>
		/// Validates a string to hex
		/// </summary>
		/// <param name="name">The name of the field to validate</param>
		/// <param name="required">If the field must be set</param>
		/// <returns>The value if validated otherwise string.Empty</returns>
		public string Hex(string name, bool required)
		{
			return Hex(name, _form[name].Value, required);
		}

        /// <summary>
        /// Validate that a string only contains letters or digits.
        /// </summary>
        /// <param name="name">Name of form parameter to validate.</param>
        /// <param name="required">Value is required.</param>
        /// <returns>value if valid; otherwise string.EmptyLanguageNode.</returns>
        public string LettersOrDigits(string name, bool required)
        {
            return LettersOrDigits(name, _form[name].Value, required);
        }

        /// <summary>
        /// Validate that a string only contains letters or digits.
        /// </summary>
        /// <param name="name">Form parameter name.</param>
        /// <returns>vaue if found; otherwise string.Empty</returns>
        public string LettersOrDigits(string name)
        {
            return LettersOrDigits(name, _form[name].Value, false);
        }

        /// <summary>
        /// Validate that a string only contains letters, digits or the specified characters
        /// </summary>
        /// <param name="name">Form parameter name.</param>
        /// <param name="required">may not be null or empty if true.</param>
        /// <param name="extraCharacters">any other allowed characters.</param>
        /// <returns>value if valid; otherwise string.Empty</returns>
        public string LettersOrDigits(string name, bool required, string extraCharacters)
        {
            return LettersOrDigits(name, _form[name].Value, required, extraCharacters);
        }

		/// <summary>
		/// Validate that a string consists of only letters (including special letters)
		/// </summary>
		/// <param name="name"></param>
		/// <param name="required">If a value must be passed</param>
		/// <returns></returns>
		public string Letters(string name, bool required)
		{
			return Letters(name, _form[name].Value, required);
		}

		/// <summary>
		/// Validate that a string consists of only letters (a-z and A-Z)
		/// </summary>
		/// <param name="name"></param>
		/// <param name="required">If a value must be passed</param>
		/// <param name="extraCharacters">A string of extra character to test against, dont forget language specific characters and spaces if wished for</param>
		/// <returns></returns>
        public string Letters(string name, bool required, string extraCharacters)
		{
            return Letters(name, _form[name].Value, required, extraCharacters);
		}

        /// <summary>
        /// Check whether the specified value is an integer.
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <returns>value if parameter contains valid characters; string.Empty if not.</returns>
        public string AlphaNumeric(string name)
        {
            return AlphaNumeric(name, _form[name].Value, false);
        }

        /// <summary>
        /// Check whether the specified value is an integer.
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="required">Paramater is required (adds an error if it's not specified)</param>
        /// <returns>value if parameter contains valid characters; string.Empty if not.</returns>
        public string AlphaNumeric(string name, bool required)
        {
            return AlphaNumeric(name, _form[name].Value, required);
        }

        /// <summary>
        /// Validate that a string only contains letters or digits or any of the <see cref="Validator.PasswordChars"/>.
        /// </summary>
        /// <param name="name">Name of form parameter to validate.</param>
        /// <param name="required">Value is required.</param>
        /// <returns>value if valid; otherwise string.Empty.</returns>
        public string Password(string name, bool required)
        {
            return Password(name, _form[name].Value, required);
        }

        /// <summary>
        /// Validate that a string only contains letters or digits or any of the <see cref="Validator.PasswordChars"/>.
        /// </summary>
        /// <param name="name">Form parameter name.</param>
        /// <returns>vaue if found; otherwise string.Empty</returns>
        public string Password(string name)
        {
            return Password(name, _form[name].Value, false);
        }

        /// <summary>
        /// Check's weather a parameter is null or not.
        /// </summary>
        /// <param name="name">Parameter in form</param>
        /// <returns>true if value is not null; otherwise false.</returns>
        public bool Required(string name)
        {
            return Required(name, _form[name].Value);
        }

        /// <summary>
        /// Validate a string value
        /// </summary>
        /// <param name="name">Name of form parameter to validate.</param>
        /// <param name="required">Value is required.</param>
        /// <returns>value if valid; otherwise string.Empty.</returns>
        [Obsolete("Use one of the more specific types instead.")]
        public string String(string name, bool required)
        {
            return String(name, _form[name].Value, required);
        }

        /// <summary>
        /// Validate a string parameter in the form
        /// </summary>
        /// <param name="name">Form parameter name.</param>
        /// <returns>vaue if found; otherwise string.Empty</returns>
        [Obsolete("Use one of the more specific types instead.")]
        public string String(string name)
        {
            return String(name, _form[name].Value, false);
        }

        /// <summary>
        /// validates email address using a regexp.
        /// </summary>
        /// <param name="name">field name</param>
        /// <param name="required">field is required (may not be null or empty).</param>
        /// <returns>value if validation is ok; otherwise string.Empty.</returns>
        public string Email(string name, bool required)
        {
            return Email(name, _form[name].Value, required);
        }

        /// <summary>
        /// Check whether the specified value is an character.
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <param name="required">Paramater is required (adds an error if it's not specified)</param>
        /// <returns>value if parameter is an int; char.MinValue if not.</returns>
        public char Char(string name, bool required)
        {
            return Char(name, _form[name].Value, required);
        }

        /// <summary>
        /// Check whether the specified value is an character.
        /// </summary>
        /// <param name="name">Name of the parameter</param>
        /// <returns>value if parameter is an int; char.MinValue if not.</returns>
        public char Char(string name)
        {
            return Char(name, _form[name].Value);
        }

        /// <summary>
        /// Checks whether a field is true (can also be in native language).
        /// </summary>
        /// <param name="name">field name</param>
        /// <param name="required">field is required (may not be null or empty).</param>
        /// <returns>true if value is true; false if value is false or if validation failed.</returns>
        /// <remarks>Check validation errors to see if error ocurred.</remarks>
        public bool Boolean(string name, bool required)
        {
            return Boolean(name, _form[name].Value, required);
        }

        /// <summary>
        /// Checks whether a field is true (can also be in native language).
        /// </summary>
        /// <param name="name">field name</param>
        /// <returns>true if value is true; false if value is false or if validation failed.</returns>
        /// <remarks>Check validation errors to see if error ocurred.</remarks>
        public bool Boolean(string name)
        {
            return Boolean(name, _form[name].Value, false);
        }

    }

}