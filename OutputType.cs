namespace CodeCombatGridmancer
{
	internal enum OutputType
	{
		None = 0,
		Text,
		Screen,
		CodeCombatGridmancer,
		/// <summary>
		/// This is a very special case which will get checked during code execution
		/// so it will likely make your processing time much slower than the others
		/// which normally take quite a bit less than one second.
		/// </summary>
		Png
	}
}