using System.Data;

namespace Inforoom.ReportSystem.DataLoaders
{
	public interface IDataLoader
	{
		DataSet LoadData();
	}
}