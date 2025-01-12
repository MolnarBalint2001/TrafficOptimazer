
using Assets.Entities;

namespace Interfaces
{

    /// <summary>
    /// XML proccessor and unit categorizer
    /// </summary>
    public interface IXmlProcessorService
    {

        /// <summary>
        /// Parsing the XML document and reforming it to proccessable data
        /// </summary>
        /// <param name="fileName">Filename</param>
        /// <returns>Map object list</returns>
        public MapData LoadXMLDocument(string fileName);
    }
}
