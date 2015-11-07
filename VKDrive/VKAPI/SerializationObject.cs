using Newtonsoft.Json;

namespace VKDrive.VKAPI
{
    public class SerializationObject
    {
        [JsonObject(MemberSerialization.OptIn)]
        public struct Album
        {
            [JsonProperty("id")]
            public int Id { get; set; }
            [JsonProperty("owner_id")]
            public int OwnerId { get; set; }
            [JsonProperty("title")]
            public string Title { get; set; }
            [JsonProperty("created")]
            public int Created { get; set; }

            [JsonProperty("Updated")]
            public int Updated { get; set; }
            [JsonProperty("size")]
            public int Size { get; set; }


        }

        [JsonObject(MemberSerialization.OptIn)]
        public struct Audio
        {
            [JsonProperty("id")]
            public int Id { get; set; }
            [JsonProperty("owner_id")]
            public int OwnerId { get; set; }
            [JsonProperty("title")]
            public string Title { get; set; }
            [JsonProperty("artist")]
            public string Artist { get; set; }
            [JsonProperty("duration")]
            public int Duration { get; set; }
            [JsonProperty("url")]
            public string Url { get; set; }
            [JsonProperty("date")]
            public string Date { get; set; }
            [JsonProperty("album_id")]
            public int AlbumId;
            [JsonProperty("genre_id")]
            public int GenreId { get; set; }
            [JsonProperty("lyrics_id")]
            public int LyricsId { get; set; }
         }

        [JsonObject(MemberSerialization.OptIn)]
        public struct Group
        {
            [JsonProperty("id")]
            public int Id;
            [JsonProperty("name")]
            public string Name;
            [JsonProperty("screen_name")]
            public string ScreenName;
            [JsonProperty("deactivated")]
            public string Deactivated;
            [JsonProperty("type")]
            public string Type;
            [JsonProperty("photo_50")]
            public string Photo50 { get; set; }
            [JsonProperty("photo_100")]
            public string Photo100 { get; set; }
            [JsonProperty("photo_200")]
            public string Photo200 { get; set; }
            [JsonProperty("description")]
            public string Description { get; set; }
            [JsonProperty("main_album_id")]
            public int MainAlbumId;
            [JsonProperty("is_favorite")]
            public int IsFavorite;
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class Photo
        {
            [JsonProperty("id")]
            public int Id { get; set; }
            [JsonProperty("owner_id")]
            public int OwnerId { get; set; }
            [JsonProperty("album_id")]
            public int AlbumId;
            [JsonProperty("created")]
            public int Created;
            

            [JsonProperty("user_id")]
            public int UserId { get; set; }
            [JsonProperty("text")]
            public string Text { get; set; }
            [JsonProperty("date")]
            public int Date { get; set; }

            [JsonProperty("photo_75")]
            public string Photo75;
            [JsonProperty("photo_130")]
            public string Photo130;
            [JsonProperty("photo_604")]
            public string Photo604;
            [JsonProperty("photo_807")]
            public string Photo807;
            [JsonProperty("photo_1280")]
            public string Photo1280;
            [JsonProperty("photo_2560")]
            public string Photo2560;

            public string GetSrc()
            {
                if (Photo2560 != null)
                    return Photo2560;
                if (Photo1280 != null)
                    return Photo1280;
                if (Photo807 != null)
                    return Photo807;
                if (Photo604 != null)
                    return Photo604;
                if (Photo130 != null)
                    return Photo130;
                return Photo75;
            }

        }

        [JsonObject(MemberSerialization.OptIn)]
        public struct User
        {
            [JsonProperty("id")]
            public int Id { get; set; }
            [JsonProperty("first_name")]
            public string FirstName { get; set; }
            [JsonProperty("last_name")]
            public string LastName { get; set; }
            [JsonProperty("deactivated")]
            public string Deactivated { get; set; }
            [JsonProperty("photo_id")]
            public string PhotoId { get; set; }
            [JsonProperty("sex")]
            public byte Sex { get; set; }
            [JsonProperty("bdate")]
            public string BirthDate { get; set; }
            


        }

    }
}
