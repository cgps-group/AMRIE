﻿namespace AMR_Engine
{
	class AntibioticComponents
	{
		#region Constants

		private const string UserDefinedAntibiotic = "X";

		#endregion

		#region Properties

		public readonly string Code;

		public readonly string Guideline;

		public readonly string TestMethod;

		#endregion

		#region Init

		/// <summary>
		/// Break the full antimicrobial code into its components.
		/// </summary>
		/// <param name="whonetAntibioticFullCode"></param>
		public AntibioticComponents(string whonetAntibioticFullCode)
		{
			char guidelineCode;
			string[] abxComponents = whonetAntibioticFullCode.Split(Constants.Delimiters.Underscore);
			Code = abxComponents[0];

			if (Code == UserDefinedAntibiotic)
			{
				// User-defined antibiotic.
				Code = string.Join(Constants.Delimiters.Underscore.ToString(), abxComponents[0], abxComponents[1]);
				guidelineCode = abxComponents[2][0];
				TestMethod = Antibiotic.TestMethods.GetTestMethodFromCode(abxComponents[2][1]);
			}
			else
			{
				// Standard WHONET antibiotic formatting.
				guidelineCode = abxComponents[1][0];
				TestMethod = Antibiotic.TestMethods.GetTestMethodFromCode(abxComponents[1][1]);
			}

			Guideline = Antibiotic.Guidelines.GetGuidelineFromCode(guidelineCode);
		}

		#endregion
	}
}
