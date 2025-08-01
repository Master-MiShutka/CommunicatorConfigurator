namespace TMP.Work.CommunicatorPSDTU.Common.Utils
{
    public static class IPAddressValidator
    {
        public static (bool, string) Validate(string ipString)
        {
            if (string.IsNullOrWhiteSpace(ipString))
            {
                return new(false, Resources.ValidatingErrors.ValueCanNotBeEmpty);
            }

            (bool IsOk, string Error) error = new(false, Resources.ValidatingErrors.YouMustEnter4NumbersDividedByPeriod);

            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4)
            {
                return error;
            }

            byte tempForParsing;

            if (splitValues.Take(3).All(r => byte.TryParse(r, out tempForParsing)))
            {
                if (byte.TryParse(splitValues[3], out tempForParsing))
                {
                    return new(true, string.Empty);
                }
                else
                {
                    ipString = splitValues[3];
                    splitValues = ipString.Split(':');

                    if (splitValues.Length != 2)
                    {
                        return error;
                    }
                    else
                    {
                        if (byte.TryParse(splitValues[0], out tempForParsing))
                        {
                            if (uint.TryParse(splitValues[1], out uint portNumber))
                            {
                                return new(true, string.Empty);
                            }
                            else
                            {
                                return new(false, Resources.ValidatingErrors.PortNumber);
                            }
                        }
                        else
                        {
                            return error;
                        }
                    }
                }
            }
            else
            {
                return error;
            }
        }
    }
}
