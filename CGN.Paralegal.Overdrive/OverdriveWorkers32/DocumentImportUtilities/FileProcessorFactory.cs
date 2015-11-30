using System;
using LexisNexis.Evolution.Infrastructure.ExceptionManagement;

namespace LexisNexis.Evolution.DocumentExtractionUtilities
{
 
    /// <summary>
    /// Handles creating objects of concrete implementation for IFileProcessor 
    /// </summary>
    public class FileProcessorFactory
    {

        /// <summary>
        /// Error codes specific to File Processor Factory
        /// </summary>
        public class ErrorCodes
        {
            /// <summary>
            /// Failure to create IFileProcessor Object
            /// </summary>
            public const string CreateIFileProcessorFailure = "CreateIFileProcessorFailure";
        }

        /// <summary>
        /// Creates IFileProcessor Objects depending on purpose fo extraction - for compound file extraction (example word document with attachments extraction) or just for text/content
        /// </summary>
        /// <param name="extractionChoices"> Extraction Chocies Enumeration depicting purpose of file extraction </param>
        /// <returns> IFileProcessor concrete implementation object </returns>
        public static IFileProcessor CreateFileProcessor(ExtractionChoices extractionChoices)
        {
            try
            {
                switch(extractionChoices)
                {
                    case ExtractionChoices.CompoundFileExtraction:
                        return new EVCorlibFileProcessorAdapter();
                        
                    default: throw new NotImplementedException();
                }
            }
            catch (EVException evException)
            {
                ((Exception)evException).Trace().Swallow();
            }
            catch (Exception exception)
            {
                const string ErrorCode = ErrorCodes.CreateIFileProcessorFailure;
                exception.AddErrorCode(ErrorCode).Trace().Swallow();
            }

            return null;
        }

        /// <summary>
        /// Purpose fo extraction
        /// </summary>
        public enum ExtractionChoices
        {
            /// <summary>
            /// For compound file extraction (example word document with attachments extraction)
            /// </summary>
            CompoundFileExtraction,
            
            /// <summary>
            /// For text/content extraction
            /// </summary>
            TextExtraction
        }
    }
}
