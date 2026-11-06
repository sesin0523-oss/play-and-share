# My Playlist

회원이 YouTube에서 음악을 검색해 플레이리스트를 만들고 공유하는 프로그램입니다.

- **서버** `MyPlaylist.Server`: ASP.NET Core 8 Web API + EF Core + MySQL, API 30개
- **클라이언트** `MyPlaylist.Client`: .NET 8 WPF

## 클라이언트 실행

1. 아래 "실행" 순서대로 서버를 먼저 켭니다.
2. Visual Studio에서 솔루션을 우클릭하고 **시작 프로젝트 구성**에서 **여러 시작 프로젝트**를 고른 뒤, 서버와 클라이언트를 모두 **시작**으로 설정합니다. 터미널에서는 서버를 켠 상태로 아래 명령을 실행합니다.

   ```bash
   dotnet run --project MyPlaylist.Client
   ```

3. 서버 주소는 기본값이 `http://localhost:5080`입니다. 다른 주소를 쓰려면 환경 변수 `MYPLAYLIST_SERVER_URL`을 지정합니다.

`seed.sql`을 실행했다면 `dummy_minji` / `password123`으로 로그인해 볼 수 있습니다.

| 폴더 | 내용 |
|---|---|
| `Services/ApiService.cs` | 서버 API 호출, 로그인 쿠키 유지 |
| `Services/Nav.cs` | 화면 이동, 로그인 상태, 공통 오류 처리 |
| `Views/` | 화면과 확인 창 |
| `Models/ApiModels.cs` | API 응답 형식 |
| `App.xaml` | 색상, 글자, 버튼, 입력칸 스타일 |

상세 API는 호출할 때마다 클릭 수가 오르므로 화면을 처음 열 때만 호출합니다. 목록의 썸네일은 첫 곡의 YouTube 썸네일을 쓰며, 곡이 없으면 빈 칸으로 표시합니다. 메인 화면의 최신순 탭은 큰 영역을 `NEW`로 표시하고 순위 번호를 빼 줍니다.

## 실행

1. MySQL 연결 정보를 user-secrets에 넣습니다. `appsettings.json`에는 실제 비밀번호를 넣지 마세요. Visual Studio에서는 프로젝트를 우클릭하고 **사용자 비밀 관리**를 눌러 `secrets.json`에 넣어도 됩니다.

   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=my_playlist;User=root;Password=본인_비밀번호;" --project MyPlaylist.Server
   ```

2. DB를 만듭니다.

   ```bash
   dotnet ef database update --project MyPlaylist.Server
   ```

   `dotnet-ef`가 없으면 `setup.sql`을 MySQL Workbench에서 전체 실행해도 됩니다. DB와 테이블이 한 번에 만들어집니다. 이전 구조의 DB가 이미 있으면 이 명령을 사용하세요.
3. YouTube 검색을 쓰려면 각자의 API 키를 user-secrets에 넣습니다. `appsettings.json`에는 넣지 마세요.

   ```bash
   dotnet user-secrets set "YouTube:ApiKey" "본인의_키" --project MyPlaylist.Server
   ```

4. `dotnet run --project MyPlaylist.Server` 후 http://localhost:5080/swagger 를 엽니다. 요청 예시는 `MyPlaylist.Server/MyPlaylist.Server.http`에 있습니다.
5. Swagger에서 `POST /api/users/signup`으로 가입하고 `POST /api/users/login`으로 로그인합니다. 로그인하면 쿠키가 저장되어 이후 요청에 자동으로 함께 전송됩니다.

## 더미 데이터와 쿼리 테스트

- `seed.sql`: 샘플 더미 데이터입니다. `setup.sql` 다음에 실행합니다. 더미 회원의 아이디는 `dummy_minji` 등이고 비밀번호는 모두 `password123`입니다. 테스트에서만 사용하세요.
- `query_test.sql`: 조회, 수정, 삭제, 제약조건 테스트 쿼리입니다. 수정과 삭제는 `ROLLBACK`으로 되돌립니다.

쿼리문과 실행 결과는 [DB 구축 및 쿼리 테스트](https://claude.ai/artifact/BXY6GPjasJpnVoY8VjTgrX) 문서에 정리되어 있습니다.

## Postman 테스트

회원, 플레이리스트, 곡의 CRUD 테스트가 `postman/MyPlaylist.postman_collection.json`에 있습니다. 자세한 사용법은 [Postman 사용 매뉴얼](https://claude.ai/artifact/JcD5EqCjhhmuh5g28AAwJ1)을 참고하세요.

1. 서버를 실행합니다. 주소가 `http://localhost:5080`이 아니면 컬렉션 변수 `baseUrl`을 바꿉니다.
2. Postman 데스크톱 앱에서 **Import**로 이 파일을 가져옵니다.
3. 컬렉션의 **Run**을 눌러 전체를 순서대로 실행합니다. 앞 요청의 결과를 뒤 요청이 사용하므로 순서를 바꾸지 마세요.

실행할 때마다 새 회원을 만들고 마지막에 탈퇴하므로 끝까지 실행하면 데이터가 남지 않습니다. 쿠키는 Postman이 자동으로 저장하고 전송합니다.

## 공통 규칙

- **로그인**: 🔒 표시가 있는 API는 로그인이 필요합니다. 로그인에 성공하면 `MyPlaylist.Session` 쿠키가 발급됩니다. 서버와 같은 주소에서 실행되는 웹 페이지는 쿠키가 자동으로 전송됩니다. 다른 주소의 웹 페이지에서 호출하려면 CORS와 쿠키 설정이 추가로 필요합니다.
- **탈퇴 후 남은 세션**: `Auth/DeletedUserMiddleware.cs`가 비로그인으로 처리합니다. 이 확인 때문에 로그인 상태의 요청은 YouTube 검색을 포함해 모두 DB 연결이 필요합니다.
- **예전 테스트 회원**: SQL로 넣은 `user_pw`가 `x`인 회원은 로그인할 수 없습니다. 회원가입 API로 새로 만드세요.

| 코드 | 의미 |
|---|---|
| 200 / 201 / 204 | 성공 / 생성됨 / 성공, 응답 본문 없음 |
| 400 | 입력값 오류 |
| 401 | 로그인 필요 또는 로그인 실패 |
| 403 | 본인이나 소유자가 아니거나 비공개 플레이리스트 |
| 404 | 대상 없음 |
| 409 | 중복 좋아요, 중복 아이디 |
| 502 / 504 | YouTube 호출 실패 / 응답 시간 초과 |
| 503 | YouTube API 키 미설정, 할당량 초과 또는 키 사용 제한 |

## 기능 단위 정리

제출과 공유용 문서는 [My Playlist 서버 기능 문서](https://claude.ai/artifact/SNu3V8VByDSrZm3L7qAf6Z)입니다. API를 바꾸면 이 README와 함께 고쳐 주세요.

| 영역 | 코드 | API 수 | 담당자 |
|---|---|---|---|
| 회원 | `UsersController` | 7 | |
| 플레이리스트 | `PlaylistsController` | 6 | |
| 곡 | `PlaylistTracksController` | 5 | |
| 좋아요 | `PlaylistLikesController`, `PlaylistsController` | 5 | |
| 통계 | `StatisticsController` | 6 | |
| YouTube 검색 | `YouTubeController`, `YouTubeSearchService` | 1 | |

### 회원

아이디(`user_name`)와 비밀번호로 가입하고 로그인합니다. 아이디는 로그인 ID 겸 닉네임이며 중복될 수 없습니다.

| Method | URL | 기능 | 실패 |
|---|---|---|---|
| POST | `/api/users/signup` | 회원가입 | 400, 409 |
| POST | `/api/users/login` | 로그인 | 400, 401 |
| POST | `/api/users/logout` | 로그아웃 | |
| GET | `/api/users/check-name?userName=alice` | 아이디 중복 확인 `{ available }` | 400 |
| GET 🔒 | `/api/users/{userId}` | 내 회원정보 조회 | 401, 403 |
| PUT 🔒 | `/api/users/{userId}` | 내 회원정보 수정 | 400, 401, 403, 409 |
| DELETE 🔒 | `/api/users/{userId}` | 회원탈퇴 | 401, 403 |

가입과 로그인은 같은 요청을 사용하고, 응답도 같은 형식입니다. 비밀번호는 응답에 넣지 않습니다.

```json
// 요청
{ "userName": "alice", "password": "password123" }
// 응답
{ "userId": 1, "userName": "alice", "createdAt": "2026-11-03T19:48:52" }
```

수정 요청은 `{ "userName": "alice2" }`입니다. 비밀번호를 바꿀 때는 `currentPassword`와 `newPassword`를 함께 보냅니다.

- **입력 제한**: 아이디는 2~50자, 비밀번호는 8~100자입니다.
- **비밀번호**: `user_pw`에는 ASP.NET Core `PasswordHasher`의 해시만 저장합니다.
- **회원탈퇴**: 회원의 플레이리스트, 곡, 좋아요는 함께 삭제되고, 다른 플레이리스트에 남긴 조회 기록은 `user_id`만 NULL이 되어 조회수가 유지됩니다.

### 플레이리스트

제목, 설명, 공개 여부를 정해 만듭니다. 공개 플레이리스트는 비회원도 조회할 수 있고, 비공개는 본인만 조회할 수 있습니다. 수정과 삭제는 소유자만 할 수 있습니다.

| Method | URL | 기능 | 실패 |
|---|---|---|---|
| POST 🔒 | `/api/playlists` | 생성 | 400, 401 |
| GET | `/api/playlists?keyword=&sort=latest\|views\|likes&page=1&size=20&offset=0` | 공개 플레이리스트 검색 (제목, 아이디) | 400 |
| GET | `/api/playlists/{playlistId}` | 상세 조회. `PLAYLIST_VIEW`에 기록 (비로그인 포함) | 403, 404 |
| GET 🔒 | `/api/users/me/playlists` | 내 플레이리스트 (비공개 포함) | 401 |
| PUT 🔒 | `/api/playlists/{playlistId}` | 수정 | 400, 401, 403, 404 |
| DELETE 🔒 | `/api/playlists/{playlistId}` | 삭제 (곡, 좋아요, 조회 기록도 함께 삭제) | 401, 403, 404 |

```json
// 생성, 수정 요청
{ "playlistTitle": "새벽 감성 노래", "description": "잠 안 올 때 듣는 노래", "isPublic": true }
// 상세 조회 응답
{
  "playlist": { "playlistId": 1, "userId": 1, "userName": "alice", "playlistTitle": "새벽 감성 노래", "description": "잠 안 올 때 듣는 노래", "isPublic": true, "viewCount": 1, "likeCount": 0, "trackCount": 1, "createdAt": "...", "updatedAt": "..." },
  "likedByMe": false,
  "tracks": []
}
```

검색 응답은 `{ page, size, offset, totalCount, items }`입니다.

- **입력 제한**: 제목은 100자, 설명은 500자까지입니다. `size`는 1~100입니다.
- **offset**: 페이지를 나누기 전에 건너뛸 개수이며 0~100입니다. 메인 화면은 1위를 `page=1&size=1`로 가져오고, 나머지는 `size=4&offset=1`로 페이지마다 4개씩 가져옵니다. 전체 페이지 수는 `(totalCount - 1) / 4`를 올림한 값입니다.
- **정렬 동점**: `likes`는 조회수, `views`는 좋아요 수가 많은 순으로 정하고, 그래도 같으면 최근에 만든 순입니다.
- **조회수**: `viewCount`는 별도 컬럼 없이 `PLAYLIST_VIEW`의 행 수로 계산합니다. 같은 회원이 반복해서 조회해도 매번 증가하고, 비로그인 조회는 `user_id`가 NULL로 저장됩니다.

### 곡

YouTube 검색 결과를 플레이리스트에 추가합니다. 같은 영상을 두 번 넣을 수 있고, 곡은 `trackId`로 구분합니다. 추가, 수정, 삭제, 순서 변경은 소유자만 할 수 있습니다.

| Method | URL | 기능 | 실패 |
|---|---|---|---|
| POST 🔒 | `/api/playlists/{playlistId}/tracks` | 추가 (맨 뒤에 추가) | 400, 401, 403, 404 |
| GET | `/api/playlists/{playlistId}/tracks` | 목록 (순서대로) | 403, 404 |
| PUT 🔒 | `/api/playlists/{playlistId}/tracks/{trackId}` | 수정 | 400, 401, 403, 404 |
| DELETE 🔒 | `/api/playlists/{playlistId}/tracks/{trackId}` | 삭제 | 401, 403, 404 |
| PATCH 🔒 | `/api/playlists/{playlistId}/tracks/order` | 순서 변경 `{ "trackIds": [3, 1, 2] }` | 400, 401, 403, 404 |

```json
// 추가, 수정 요청
{ "youtubeVideoId": "BzYnNdJhZQw", "musicTitle": "밤편지", "artist": "아이유" }
// 추가 응답
{ "trackId": 1, "playlistId": 1, "youtubeVideoId": "BzYnNdJhZQw", "musicTitle": "밤편지", "artist": "아이유", "youtubeUrl": "https://www.youtube.com/watch?v=BzYnNdJhZQw", "sortOrder": 1, "addedAt": "...", "thumbnailUrl": "https://i.ytimg.com/vi/BzYnNdJhZQw/mqdefault.jpg" }
```

- **서버가 정하는 값**: `youtube_url`은 `youtubeVideoId`로 만들고 `sort_order`는 맨 뒤 순서로 정합니다. 클라이언트가 보내지 않습니다.
- **썸네일**: 응답의 `thumbnailUrl`은 DB에 저장하지 않고 영상 ID로 만듭니다.
- **순서 변경**: 플레이리스트의 모든 `trackId`를 바뀐 순서대로 한 번씩 보내야 합니다. 일부만 보내면 400입니다.

### 좋아요

회원은 같은 플레이리스트에 한 번만 좋아요를 누를 수 있고, 본인 플레이리스트에도 누를 수 있습니다. 중복은 `UNIQUE(user_id, playlist_id)`로 막습니다.

| Method | URL | 기능 | 실패 |
|---|---|---|---|
| POST 🔒 | `/api/playlists/{playlistId}/likes` | 등록 | 401, 403, 404, 409 |
| DELETE 🔒 | `/api/playlists/{playlistId}/likes` | 취소 | 401, 404 |
| GET 🔒 | `/api/playlists/{playlistId}/likes/me` | 내 좋아요 여부 `{ liked }` | 401, 403, 404 |
| GET | `/api/playlists/{playlistId}/likes/count` | 좋아요 수 `{ likeCount }` | 403, 404 |
| GET 🔒 | `/api/users/me/likes` | 내가 좋아요한 목록 (비공개로 바뀐 남의 플레이리스트 제외) | 401 |

### 통계

별도 컬럼 없이 요청할 때마다 DB의 행 수를 세어 계산합니다.

| Method | URL | 응답 |
|---|---|---|
| GET | `/api/statistics/users/count` | `{ userCount }` |
| GET | `/api/statistics/playlists/count` | `{ playlistCount }` (비공개 포함) |
| GET | `/api/statistics/playlists/views` | `{ totalViewCount }` |
| GET | `/api/statistics/playlists/likes` | `{ totalLikeCount }` |
| GET | `/api/statistics/playlists/popular?sort=likes\|views&limit=10` | 공개 플레이리스트 순위 (`limit`은 1~50) |
| GET 🔒 | `/api/statistics/users/me` | `{ playlistCount, trackCount, givenLikeCount, receivedLikeCount, totalViewCount }` |

### YouTube 검색

| Method | URL | 기능 | 실패 |
|---|---|---|---|
| GET 🔒 | `/api/youtube/search?keyword=&maxResults=10` | 음악 검색 (`maxResults`는 1~25) | 400, 401, 502, 503, 504 |

```json
[{ "youtubeVideoId": "BzYnNdJhZQw", "musicTitle": "밤편지", "artist": "아이유", "thumbnailUrl": "...", "youtubeUrl": "..." }]
```

응답의 `youtubeVideoId`, `musicTitle`, `artist`를 곡 추가 API에 그대로 보내면 됩니다.

클라이언트는 YouTube를 직접 호출하지 말고 이 API를 사용해 주세요. 키는 서버에만 둡니다. 검색 1회에 일일 할당량(기본 10,000) 중 100을 사용하므로 하루 약 100회까지 검색할 수 있습니다.

## 요구사항 대조

요구사항 명세서의 22개 기능은 모두 서버 API가 있고, WPF 클라이언트 화면에도 구현되어 있습니다. 명세서에는 웹 앱으로 적혀 있지만 클라이언트는 WPF로 만들었습니다.

| ID | 기능 | API | 화면 필요 |
|---|---|---|---|
| M-01 | 회원가입 | `POST /api/users/signup` | |
| M-02 | 로그인 | `POST /api/users/login` | |
| M-03 | 로그아웃 | `POST /api/users/logout` | |
| S-01 | 음악 검색 | `GET /api/youtube/search` | 결과 표시 |
| S-02 | 링크·썸네일 표시 | 응답의 `youtubeUrl`, `thumbnailUrl` | 썸네일, 링크 표시 |
| P-01 | 생성 | `POST /api/playlists` | |
| P-02 | 수정 | `PUT /api/playlists/{playlistId}` | |
| P-03 | 삭제 | `DELETE /api/playlists/{playlistId}` | |
| P-04 | 공개/비공개 설정 | `isPublic` 값 | |
| P-05 | 내 플레이리스트 목록 | `GET /api/users/me/playlists` | 마이페이지 |
| P-06 | 상세 조회 | `GET /api/playlists/{playlistId}` | 곡 클릭 시 유튜브 이동 |
| P-07 | 검색 | `GET /api/playlists?keyword=` | |
| P-08 | 목록 정렬 | `GET /api/playlists?sort=` | |
| T-01 | 곡 추가 | `POST /api/playlists/{playlistId}/tracks` | 검색 결과에서 선택 |
| T-02 | 곡 삭제 | `DELETE /api/playlists/{playlistId}/tracks/{trackId}` | |
| T-03 | 곡 순서 변경 | `PATCH /api/playlists/{playlistId}/tracks/order` | |
| L-01 | 좋아요 표시 | `POST /api/playlists/{playlistId}/likes` | |
| L-02 | 좋아요 취소 | `DELETE /api/playlists/{playlistId}/likes` | |
| L-03 | 좋아요한 목록 | `GET /api/users/me/likes` | |
| V-01 | 클릭 수 기록 | `GET /api/playlists/{playlistId}` | |
| V-02 | 클릭 수 표시 | 응답의 `viewCount` | |
| V-03 | 인기순 반영 | `sort=views`, `GET /api/statistics/playlists/popular` | |

명세서의 22개 기능 외에 회원정보 조회·수정, 회원탈퇴, 아이디 중복 확인, 곡 수정, 통계 API도 구현되어 있습니다.

## 테스트 현황

| 대상 | 방법 | 결과 |
|---|---|---|
| 회원, 플레이리스트, 곡 CRUD 16개 API | Postman Runner | 요청 38개, 테스트 57개 통과 |
| 전체 30개 API | 임시 DB에서 스크립트 실행 | 검사 166개 통과 |
| YouTube 검색 | 실제 키로 호출 | 200 확인 |
| WPF 클라이언트 | 임시 DB와 서버에서 UI 자동화로 실행 | 주요 화면 동작 확인 ("YouTube에서 보기" 버튼 제외) |

YouTube 할당량 초과(503)와 시간 초과(504)는 실제로 재현해 확인하지 못했습니다.
