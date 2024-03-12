using System;
using Api.Data.Models;
using Api.Service;
using AutoMapper;

namespace Api.TypeConverters
{
    public class ClouldFileTypeConverter : ITypeConverter<Image, Uri>
    {
        private readonly CdnService _cdnService;

        public ClouldFileTypeConverter(CdnService cdnService)
        {
            _cdnService = cdnService ?? throw new ArgumentNullException(nameof(cdnService));
        }

        public Uri? Convert(Image source, Uri destination, ResolutionContext context)
        {
            return _cdnService.getCdnPath(source);
        }
    }
}