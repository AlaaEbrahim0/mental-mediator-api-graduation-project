namespace Domain.Enums;

public enum DoctorSpecialization
{
	ClinicalPsychology,
	CounselingPsychology,
	HealthPsychology,
	NeuroPsychology,
	ForensicPsychology,
	SchoolPsychology,
	SocialPsychology
}

public static int ArrayChallenge(int[] arr)
{

	int maxNumber = int.MinValue;
	for (int i = 0; i < arr.Length; ++i)
	{
		if (arr[i] >= maxNumber)
		{
			maxNumber = arr[i];
		}
	}
	int elementsSumExceptMax = 0;
	for (int i = 0; i < arr.Length; ++i)
	{
		if (arr[i] != maxNumber)
		{
			elementsSumExceptMax += arr[i];
		}
	}
	if (maxNumber == elementsSumExceptMax)
	{
		return 1;
	}
	return 0;
}
