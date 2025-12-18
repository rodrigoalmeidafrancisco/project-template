namespace Shared.Settings
{
    public class SettingAppAplication
    {
        public SettingAppAplication()
        {

        }

        public string _Environment { get; set; }
        public List<string> AccessPolicy { get; set; }
        public List<KeyValuePair<string, string[]>> ListAccessPolicy => SelectAccessPolicyList();
        public string Identifier { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool UseProxy { get; set; }
        public string WebUri { get; set; }

        private List<KeyValuePair<string, string[]>> SelectAccessPolicyList()
        {
            var result = new List<KeyValuePair<string, string[]>>();

            if (AccessPolicy != null && AccessPolicy.Count != 0)
            {
                string[] listAccessPolicySplit;
                string key;
                string[] value;

                foreach (var item in AccessPolicy)
                {
                    listAccessPolicySplit = item.Split('|');
                    key = listAccessPolicySplit[0];
                    value = listAccessPolicySplit[1].Split(' ');

                    for (int i = 0; i < value.Length; i++)
                    {
                        value[i] = value[i].Trim();
                    }

                    result.Add(new KeyValuePair<string, string[]>(key.Trim(), value));
                }
            }

            return result;
        }


    }
}
