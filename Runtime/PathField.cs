namespace InspectorPathField
{
    [System.Serializable]
    public class PathField
    {
        public string AssetPath;

        public override string ToString()
        {
            return AssetPath;
        }


	public static implicit operator string(PathField pathField)
    	{
        	return pathField?.AssetPath;
    	}
    }
}
