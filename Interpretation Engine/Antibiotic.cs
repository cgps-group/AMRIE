﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AMR_Engine
{
	public class Antibiotic
	{
		#region Constants

		/// <summary>
		/// Boolean fields in our text table are indicated with an X or a blank.
		/// </summary>
		private const string X = "X";

		public class TestMethods
		{
			public const string Disk = "DISK";
			public const string MIC = "MIC";
			public const string ETest = "ETEST";

			// MIC breakpoint rows apply to ETest values as well.
			public static string GetTestMethodFromCode(char testMethodCode)
			{
				switch (testMethodCode)
				{
					case TestMethodCodes.Disk:
						return Disk;

					case TestMethodCodes.MIC:
					case TestMethodCodes.ETest:
						return MIC;
				}

				throw new ArgumentException(testMethodCode.ToString());
			}
		}

		public class TestMethodCodes
		{
			public const char Disk = 'D';
			public const char MIC = 'M';
			public const char ETest = 'E';
		}

		public class Guidelines
		{
			public static readonly string CLSI = "CLSI";
			public static readonly string EUCAST = "EUCAST";
			public static readonly string SFM = "SFM";
			public static readonly string SRGA = "SRGA";
			public static readonly string BSAC = "BSAC";
			public static readonly string DIN = "DIN";
			public static readonly string NEO = "NEO";
			public static readonly string AFA = "AFA";

			/// <summary>
			/// Expand the guideline code used in the antibiotic column name to the longer abbreviation.
			/// </summary>
			/// <param name="guidelineCode"></param>
			/// <returns></returns>
			/// <exception cref="ArgumentException"></exception>
			public static string GetGuidelineFromCode(char guidelineCode)
			{
				switch (guidelineCode)
				{
					case GuidelineCodes.CLSI:
						return CLSI;

					case GuidelineCodes.EUCAST:
						return EUCAST;

					case GuidelineCodes.SFM:
						return SFM;

					case GuidelineCodes.SRGA:
						return SRGA;

					case GuidelineCodes.DIN:
						return DIN;

					case GuidelineCodes.NEO:
						return NEO;

					case GuidelineCodes.BSAC:
						return BSAC;

					case GuidelineCodes.AFA:
						return AFA;

					default:
						throw new ArgumentException();
				}
			}
		}

		/// <summary>
		/// These short codes are used in the antibiotic column names.
		/// </summary>
		public class GuidelineCodes
		{
			public const char CLSI = 'N';
			public const char EUCAST = 'E';
			public const char SFM = 'F';
			public const char SRGA = 'S';
			public const char DIN = 'D';
			public const char NEO = 'T';
			public const char BSAC = 'B';
			public const char AFA = 'A';

			public static readonly char[] AllCodes = { CLSI, EUCAST, SFM, SRGA, DIN, NEO, BSAC, AFA };
		}

		/// <summary>
		/// All antibiotics known to the system.
		/// </summary>
		public static readonly List<Antibiotic> AllAntibiotics = LoadAntibiotics();

		// Prefiltered drug lists for performance.
		public static readonly List<string> CEPH3_AntibioticCodes =
			AllAntibiotics.Where(a => a.PROF_CLASS == ExpertInterpretationRule.PROF_CLASS.CEPH3)
			.Select(a => a.WHONET_ABX_CODE)
			.Distinct().ToList();

		private static readonly HashSet<string> MRS_Classes = new HashSet<string>() { "Penicillins", "Cephems", "Cephems-Oral", "Monobactams", "Penems", "Beta-lactam+Inhibitors", "Beta-lactamase inhibitors" };
		public static readonly List<string> MRS_Antibiotics_Except_CPT_BPR =
			AllAntibiotics.Where(a => MRS_Classes.Contains(a.CLASS))
			.Select(a => a.WHONET_ABX_CODE)
			.Except(new List<string> { "CPT", "BPR" })
			.Distinct().ToList();

		private static readonly HashSet<string> ICR_Classes = new HashSet<string>() { "Macrolides", "Lincosamides", "Streptogramins" };
		public static readonly List<string> ICR_Antibiotics =
			AllAntibiotics.Where(a => ICR_Classes.Contains(a.CLASS))
			.Select(a => a.WHONET_ABX_CODE)
			.Distinct().ToList();

		#endregion

		#region Init

		/// <summary>
		/// Used only to support static nameof()
		/// </summary>
		private Antibiotic() { }

		/// <summary>
		/// Setup for a single antibiotic row.
		/// </summary>
		/// <param name="WHONET_ABX_CODE_"></param>
		/// <param name="WHO_CODE_"></param>
		/// <param name="DIN_CODE_"></param>
		/// <param name="JAC_CODE_"></param>
		/// <param name="EUCAST_CODE_"></param>
		/// <param name="USER_CODE_"></param>
		/// <param name="ANTIBIOTIC_"></param>
		/// <param name="GUIDELINES_"></param>
		/// <param name="CLSI_"></param>
		/// <param name="EUCAST_"></param>
		/// <param name="SFM_"></param>
		/// <param name="SRGA_"></param>
		/// <param name="BSAC_"></param>
		/// <param name="DIN_"></param>
		/// <param name="NEO_"></param>
		/// <param name="AFA_"></param>
		/// <param name="ABX_NUMBER_"></param>
		/// <param name="POTENCY_"></param>
		/// <param name="ATC_CODE_"></param>
		/// <param name="CLASS_"></param>
		/// <param name="PROF_CLASS_"></param>
		/// <param name="CIA_CATEGORY_"></param>
		/// <param name="CLSI_ORDER_"></param>
		/// <param name="EUCAST_ORDER_"></param>
		/// <param name="HUMAN_"></param>
		/// <param name="VETERINARY_"></param>
		/// <param name="ANIMAL_GP_"></param>
		/// <param name="LOINCCOMP_"></param>
		/// <param name="LOINCGEN_"></param>
		/// <param name="LOINCDISK_"></param>
		/// <param name="LOINCMIC_"></param>
		/// <param name="LOINCETEST_"></param>
		/// <param name="LOINCSLOW_"></param>
		/// <param name="LOINCAFB_"></param>
		/// <param name="LOINCSBT_"></param>
		/// <param name="LOINCMLC_"></param>
		/// <param name="DATE_ENTERED_"></param>
		/// <param name="DATE_MODIFIED_"></param>
		/// <param name="COMMENTS_"></param>
		public Antibiotic(string WHONET_ABX_CODE_, string WHO_CODE_, string DIN_CODE_,
			string JAC_CODE_, string EUCAST_CODE_, string USER_CODE_,
			string ANTIBIOTIC_, string GUIDELINES_, bool CLSI_,
			bool EUCAST_, bool SFM_, bool SRGA_,
			bool BSAC_, bool DIN_, bool NEO_,
			bool AFA_, string ABX_NUMBER_, string POTENCY_,
			string ATC_CODE_, string CLASS_, string PROF_CLASS_, string CIA_CATEGORY_,
			string CLSI_ORDER_, string EUCAST_ORDER_, bool HUMAN_,
			bool VETERINARY_, bool ANIMAL_GP_, string LOINCCOMP_,
			string LOINCGEN_, string LOINCDISK_, string LOINCMIC_,
			string LOINCETEST_, string LOINCSLOW_, string LOINCAFB_,
			string LOINCSBT_, string LOINCMLC_, DateTime DATE_ENTERED_,
			DateTime DATE_MODIFIED_, string COMMENTS_)
		{
			WHONET_ABX_CODE = WHONET_ABX_CODE_;
			WHO_CODE = WHO_CODE_;
			DIN_CODE = DIN_CODE_;
			JAC_CODE = JAC_CODE_;
			EUCAST_CODE = EUCAST_CODE_;
			USER_CODE = USER_CODE_;
			ANTIBIOTIC = ANTIBIOTIC_;
			GUIDELINES = GUIDELINES_;
			CLSI = CLSI_;
			EUCAST = EUCAST_;
			SFM = SFM_;
			SRGA = SRGA_;
			BSAC = BSAC_;
			DIN = DIN_;
			NEO = NEO_;
			NEO = AFA_;
			ABX_NUMBER = ABX_NUMBER_;
			ABX_NUMBER = POTENCY_;
			ATC_CODE = ATC_CODE_;
			CLASS = CLASS_;
			PROF_CLASS = PROF_CLASS_;
			CIA_CATEGORY = CIA_CATEGORY_;
			CLSI_ORDER = CLSI_ORDER_;
			EUCAST_ORDER = EUCAST_ORDER_;
			HUMAN = HUMAN_;
			VETERINARY = VETERINARY_;
			ANIMAL_GP = ANIMAL_GP_;
			LOINCCOMP = LOINCCOMP_;
			LOINCGEN = LOINCGEN_;
			LOINCDISK = LOINCDISK_;
			LOINCMIC = LOINCMIC_;
			LOINCETEST = LOINCETEST_;
			LOINCSLOW = LOINCSLOW_;
			LOINCAFB = LOINCAFB_;
			LOINCSBT = LOINCSBT_;
			LOINCMLC = LOINCMLC_;
			DATE_ENTERED = DATE_ENTERED_;
			DATE_MODIFIED = DATE_MODIFIED_;
			COMMENTS = COMMENTS_;
		}

		#endregion

		#region Properties

		public readonly string WHONET_ABX_CODE;
		public readonly string WHO_CODE;
		public readonly string DIN_CODE;
		public readonly string JAC_CODE;
		public readonly string EUCAST_CODE;
		public readonly string USER_CODE;
		public readonly string ANTIBIOTIC;
		public readonly string GUIDELINES;
		public readonly bool CLSI;
		public readonly bool EUCAST;
		public readonly bool SFM;
		public readonly bool SRGA;
		public readonly bool BSAC;
		public readonly bool DIN;
		public readonly bool NEO;
		public readonly bool AFA;
		public readonly string ABX_NUMBER;
		public readonly string POTENCY;
		public readonly string ATC_CODE;
		public readonly string CLASS;
		public readonly string PROF_CLASS;
		public readonly string CIA_CATEGORY;
		public readonly string CLSI_ORDER;
		public readonly string EUCAST_ORDER;
		public readonly bool HUMAN;
		public readonly bool VETERINARY;
		public readonly bool ANIMAL_GP;
		public readonly string LOINCCOMP;
		public readonly string LOINCGEN;
		public readonly string LOINCDISK;
		public readonly string LOINCMIC;
		public readonly string LOINCETEST;
		public readonly string LOINCSLOW;
		public readonly string LOINCAFB;
		public readonly string LOINCSBT;
		public readonly string LOINCMLC;
		public readonly DateTime DATE_ENTERED;
		public readonly DateTime DATE_MODIFIED;
		public readonly string COMMENTS;

		#endregion

		#region Public

		public static string ShortCode(string fullAntibioticCode)
		{
			return fullAntibioticCode.Substring(0, 3);
		}

		#endregion

		#region Private

		/// <summary>
		/// Load the antibiotic text file into a list of objects.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="FileNotFoundException"></exception>
		private static List<Antibiotic> LoadAntibiotics()
		{
			List<Antibiotic> antibiotics = new List<Antibiotic>();
			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			using (Stream stream = assembly.GetManifestResourceStream("AMR_Engine.Resources.Antibiotics.txt"))
			using (StreamReader reader = new StreamReader(stream))
			{
				string headerLine = reader.ReadLine();
				Dictionary<string, int> headerMap = IO_Library.GetHeaders(headerLine);

				while (!reader.EndOfStream)
				{
					string thisLine = reader.ReadLine();
					string[] values = thisLine.Split(Constants.Delimiters.TabChar);

					DateTime tempEntered = DateTime.MinValue;
					if (!string.IsNullOrWhiteSpace(values[headerMap[nameof(DATE_ENTERED)]]))
						tempEntered = DateTime.Parse(values[headerMap[nameof(DATE_ENTERED)]], System.Globalization.CultureInfo.InvariantCulture);

					DateTime tempModified = DateTime.MinValue;
					if (!string.IsNullOrWhiteSpace(values[headerMap[nameof(DATE_MODIFIED)]]))
						tempModified = DateTime.Parse(values[headerMap[nameof(DATE_MODIFIED)]], System.Globalization.CultureInfo.InvariantCulture);

					Antibiotic newAntibiotic = new Antibiotic(values[headerMap[nameof(WHONET_ABX_CODE)]], values[headerMap[nameof(WHO_CODE)]], 
						values[headerMap[nameof(DIN_CODE)]], values[headerMap[nameof(JAC_CODE)]], values[headerMap[nameof(EUCAST_CODE)]],
						values[headerMap[nameof(USER_CODE)]], values[headerMap[nameof(ANTIBIOTIC)]], values[headerMap[nameof(GUIDELINES)]],
						values[headerMap[nameof(CLSI)]] == X, values[headerMap[nameof(EUCAST)]] == X, values[headerMap[nameof(SFM)]] == X, 
						values[headerMap[nameof(SRGA)]] == X, values[headerMap[nameof(BSAC)]] == X, values[headerMap[nameof(DIN)]] == X, 
						values[headerMap[nameof(NEO)]] == X, values[headerMap[nameof(AFA)]] == X, values[headerMap[nameof(ABX_NUMBER)]], 
						values[headerMap[nameof(POTENCY)]], values[headerMap[nameof(ATC_CODE)]], values[headerMap[nameof(CLASS)]], 
						values[headerMap[nameof(PROF_CLASS)]], values[headerMap[nameof(CIA_CATEGORY)]],
						values[headerMap[nameof(CLSI_ORDER)]], values[headerMap[nameof(EUCAST_ORDER)]], values[headerMap[nameof(HUMAN)]] == X,
						values[headerMap[nameof(VETERINARY)]] == X, values[headerMap[nameof(ANIMAL_GP)]] == X, values[headerMap[nameof(LOINCCOMP)]],
						values[headerMap[nameof(LOINCGEN)]], values[headerMap[nameof(LOINCDISK)]], values[headerMap[nameof(LOINCMIC)]],
						values[headerMap[nameof(LOINCETEST)]], values[headerMap[nameof(LOINCSLOW)]], values[headerMap[nameof(LOINCAFB)]],
						values[headerMap[nameof(LOINCSBT)]], values[headerMap[nameof(LOINCMLC)]], tempEntered, tempModified, values[headerMap[nameof(COMMENTS)]]);

					antibiotics.Add(newAntibiotic);
				}
			}				

			return antibiotics;
		}

		#endregion
	}
}
