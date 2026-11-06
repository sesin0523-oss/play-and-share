-- My Playlist DB 셋업
-- MySQL Workbench에서 전체 실행(Ctrl+Shift+Enter)하면 DB와 테이블이 만들어진다.
-- 이미 있는 테이블과 데이터는 지우거나 바꾸지 않으므로 여러 번 실행해도 안전하다.
-- 이전 구조의 my_playlist DB가 이미 있으면 이 파일 대신 dotnet ef database update를 사용한다.

CREATE DATABASE IF NOT EXISTS my_playlist
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_0900_ai_ci;

USE my_playlist;

-- 회원
CREATE TABLE IF NOT EXISTS `USER` (
    `user_id` int NOT NULL AUTO_INCREMENT,
    `user_name` varchar(50) NOT NULL,
    `user_pw` varchar(255) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    PRIMARY KEY (`user_id`),
    UNIQUE KEY `IX_USER_user_name` (`user_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 플레이리스트
CREATE TABLE IF NOT EXISTS `PLAYLIST` (
    `playlist_id` int NOT NULL AUTO_INCREMENT,
    `user_id` int NOT NULL,
    `playlist_title` varchar(100) NOT NULL,
    `description` varchar(500) DEFAULT NULL,
    `is_public` tinyint(1) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    PRIMARY KEY (`playlist_id`),
    KEY `IX_PLAYLIST_user_id` (`user_id`),
    CONSTRAINT `FK_PLAYLIST_USER_user_id` FOREIGN KEY (`user_id`)
        REFERENCES `USER` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 플레이리스트의 곡
CREATE TABLE IF NOT EXISTS `PLAYLIST_TRACK` (
    `track_id` int NOT NULL AUTO_INCREMENT,
    `playlist_id` int NOT NULL,
    `youtube_video_id` varchar(30) NOT NULL,
    `music_title` varchar(200) NOT NULL,
    `artist` varchar(200) DEFAULT NULL,
    `youtube_url` varchar(500) NOT NULL,
    `sort_order` int NOT NULL,
    `added_at` datetime(6) NOT NULL,
    PRIMARY KEY (`track_id`),
    KEY `IX_PLAYLIST_TRACK_playlist_id` (`playlist_id`),
    CONSTRAINT `FK_PLAYLIST_TRACK_PLAYLIST_playlist_id` FOREIGN KEY (`playlist_id`)
        REFERENCES `PLAYLIST` (`playlist_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 좋아요 (한 회원은 같은 플레이리스트에 1번만)
CREATE TABLE IF NOT EXISTS `PLAYLIST_LIKE` (
    `like_id` int NOT NULL AUTO_INCREMENT,
    `user_id` int NOT NULL,
    `playlist_id` int NOT NULL,
    `created_at` datetime(6) NOT NULL,
    PRIMARY KEY (`like_id`),
    UNIQUE KEY `IX_PLAYLIST_LIKE_user_id_playlist_id` (`user_id`, `playlist_id`),
    KEY `IX_PLAYLIST_LIKE_playlist_id` (`playlist_id`),
    CONSTRAINT `FK_PLAYLIST_LIKE_PLAYLIST_playlist_id` FOREIGN KEY (`playlist_id`)
        REFERENCES `PLAYLIST` (`playlist_id`) ON DELETE CASCADE,
    CONSTRAINT `FK_PLAYLIST_LIKE_USER_user_id` FOREIGN KEY (`user_id`)
        REFERENCES `USER` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 클릭 기록 (비회원 클릭과 탈퇴한 회원의 클릭은 user_id가 NULL)
CREATE TABLE IF NOT EXISTS `PLAYLIST_VIEW` (
    `view_id` int NOT NULL AUTO_INCREMENT,
    `user_id` int DEFAULT NULL,
    `playlist_id` int NOT NULL,
    `viewed_at` datetime(6) NOT NULL,
    PRIMARY KEY (`view_id`),
    KEY `IX_PLAYLIST_VIEW_playlist_id` (`playlist_id`),
    KEY `IX_PLAYLIST_VIEW_user_id` (`user_id`),
    CONSTRAINT `FK_PLAYLIST_VIEW_PLAYLIST_playlist_id` FOREIGN KEY (`playlist_id`)
        REFERENCES `PLAYLIST` (`playlist_id`) ON DELETE CASCADE,
    CONSTRAINT `FK_PLAYLIST_VIEW_USER_user_id` FOREIGN KEY (`user_id`)
        REFERENCES `USER` (`user_id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 마이그레이션 기록
-- 이 기록이 있어야 나중에 dotnet ef database update가 테이블을 다시 만들려다 실패하지 않는다.
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES
    ('20261103011257_InitialCreate', '8.0.33'),
    ('20261103011818_ApplyFinalErd', '8.0.33'),
    ('20261103012132_AllowAnonymousView', '8.0.33'),
    ('20261103014459_KeepViewsOnUserDelete', '8.0.33');

-- 확인: 테이블 5개와 __efmigrationshistory가 보이면 성공
-- 회원은 비밀번호 해시가 필요하므로 SQL이 아니라 회원가입 API로 만든다.
SHOW TABLES;
