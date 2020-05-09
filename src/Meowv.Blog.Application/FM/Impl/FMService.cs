﻿using Meowv.Blog.Application.Caching.FM;
using Meowv.Blog.Application.Contracts.FM;
using Meowv.Blog.Domain.Configurations;
using Meowv.Blog.ToolKits.Base;
using Meowv.Blog.ToolKits.Extensions;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static Meowv.Blog.Domain.Shared.MeowvBlogConsts;

namespace Meowv.Blog.Application.FM.Impl
{
    public class FMService : ServiceBase, IFMService
    {
        private readonly IHttpClientFactory _httpClient;
        private readonly IFMCacheService _fmCacheService;

        public FMService(IHttpClientFactory httpClient,
                         IFMCacheService fmCacheService)
        {
            _httpClient = httpClient;
            _fmCacheService = fmCacheService;
        }

        /// <summary>
        /// 获取专辑分类
        /// </summary>
        /// <param name="specific"></param>
        /// <returns></returns>
        public async Task<ServiceResult<IEnumerable<ChannelDto>>> GetChannelsAsync(string specific)
        {
            var result = new ServiceResult<IEnumerable<ChannelDto>>();

            if (specific.IsNotNullOrEmpty() && specific != "all")
            {
                result.IsFailed(ResponseText.PARAMETER_ERROR);
                return result;
            }

            return await _fmCacheService.GetChannelsAsync(specific, async () =>
            {
                var list = new List<ChannelDto>();

                using var client = _httpClient.CreateClient();

                var response = await client.GetStringAsync(AppSettings.FMApi.Channels.FormatWith(specific));
                var channels = response.FromJson<dynamic>()["data"]["channels"];

                if (specific == "all")
                {
                    var _channels = new List<dynamic>
                    {
                        channels["scenario"], channels["language"], channels["artist"],
                        channels["track"], channels["brand"], channels["genre"]
                    };
                    for (int i = 0; i < _channels.Count; i++)
                    {
                        foreach (var item in _channels[i])
                        {
                            list.Add(new ChannelDto
                            {
                                Id = item["id"],
                                Name = item["name"],
                                Intro = item["intro"],
                                Banner = item["banner"],
                                Cover = item["cover"]
                            });
                        }
                    }
                }
                else
                {
                    foreach (var item in channels)
                    {
                        list.Add(new ChannelDto
                        {
                            Id = item["id"],
                            Name = item["name"],
                            Intro = item["intro"],
                            Banner = item["banner"],
                            Cover = item["cover"]
                        });
                    }
                }

                result.IsSuccess(list);
                return result;
            });
        }

        /// <summary>
        /// 根据专辑分类获取随机歌曲
        /// </summary>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public async Task<ServiceResult<IEnumerable<FMDto>>> GetFmAsync(int channelId)
        {
            var result = new ServiceResult<IEnumerable<FMDto>>();

            using var client = _httpClient.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, AppSettings.FMApi.Song.FormatWith(channelId));
            request.Headers.Add("Cookie", "flag=\"ok\"; bid=I8DWNdOlti8; ac=\"1588990113\";");

            var response = await client.SendAsync(request);

            var json = await response.Content.ReadAsStringAsync();
            var playlist = json.FromJson<dynamic>()["song"];

            var list = new List<FMDto>();

            foreach (var item in playlist)
            {
                list.Add(new FMDto
                {
                    AlbumTitle = item["albumtitle"],
                    Artist = item["artist"],
                    Picture = item["picture"],
                    Url = item["url"],
                });
            }

            result.IsSuccess(list);
            return result;
        }
    }
}