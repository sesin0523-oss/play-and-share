CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103011257_InitialCreate') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103011257_InitialCreate') THEN

    CREATE TABLE `USER` (
        `user_id` int NOT NULL AUTO_INCREMENT,
        `user_name` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `user_pw` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `created_at` datetime(6) NOT NULL,
        CONSTRAINT `PK_USER` PRIMARY KEY (`user_id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103011257_InitialCreate') THEN

    CREATE TABLE `PLAYLIST` (
        `playlist_id` int NOT NULL AUTO_INCREMENT,
        `user_id` int NOT NULL,
        `playlist_title` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `description` varchar(500) CHARACTER SET utf8mb4 NULL,
        `cover_image` varchar(500) CHARACTER SET utf8mb4 NULL,
        `is_public` tinyint(1) NOT NULL,
        `view_count` int NOT NULL,
        `created_at` datetime(6) NOT NULL,
        `updated_at` datetime(6) NOT NULL,
        CONSTRAINT `PK_PLAYLIST` PRIMARY KEY (`playlist_id`),
        CONSTRAINT `FK_PLAYLIST_USER_user_id` FOREIGN KEY (`user_id`) REFERENCES `USER` (`user_id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103011257_InitialCreate') THEN

    CREATE TABLE `PLAYLIST_LIKE` (
        `like_id` int NOT NULL AUTO_INCREMENT,
        `user_id` int NOT NULL,
        `playlist_id` int NOT NULL,
        `created_at` datetime(6) NOT NULL,
        CONSTRAINT `PK_PLAYLIST_LIKE` PRIMARY KEY (`like_id`),
        CONSTRAINT `FK_PLAYLIST_LIKE_PLAYLIST_playlist_id` FOREIGN KEY (`playlist_id`) REFERENCES `PLAYLIST` (`playlist_id`) ON DELETE CASCADE,
        CONSTRAINT `FK_PLAYLIST_LIKE_USER_user_id` FOREIGN KEY (`user_id`) REFERENCES `USER` (`user_id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103011257_InitialCreate') THEN

    CREATE TABLE `PLAYLIST_TRACK` (
        `track_id` int NOT NULL AUTO_INCREMENT,
        `playlist_id` int NOT NULL,
        `youtube_video_id` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `music_title` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `artist` varchar(200) CHARACTER SET utf8mb4 NULL,
        `thumbnail_url` varchar(500) CHARACTER SET utf8mb4 NULL,
        `youtube_url` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `sort_order` int NOT NULL,
        `added_at` datetime(6) NOT NULL,
        CONSTRAINT `PK_PLAYLIST_TRACK` PRIMARY KEY (`track_id`),
        CONSTRAINT `FK_PLAYLIST_TRACK_PLAYLIST_playlist_id` FOREIGN KEY (`playlist_id`) REFERENCES `PLAYLIST` (`playlist_id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103011257_InitialCreate') THEN

    CREATE INDEX `IX_PLAYLIST_user_id` ON `PLAYLIST` (`user_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103011257_InitialCreate') THEN

    CREATE INDEX `IX_PLAYLIST_LIKE_playlist_id` ON `PLAYLIST_LIKE` (`playlist_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103011257_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_PLAYLIST_LIKE_user_id_playlist_id` ON `PLAYLIST_LIKE` (`user_id`, `playlist_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103011257_InitialCreate') THEN

    CREATE INDEX `IX_PLAYLIST_TRACK_playlist_id` ON `PLAYLIST_TRACK` (`playlist_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103011257_InitialCreate') THEN

    CREATE UNIQUE INDEX `IX_USER_user_name` ON `USER` (`user_name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103011257_InitialCreate') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261103011257_InitialCreate', '8.0.33');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103011818_ApplyFinalErd') THEN

    ALTER TABLE `PLAYLIST_TRACK` DROP COLUMN `thumbnail_url`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103011818_ApplyFinalErd') THEN

    ALTER TABLE `PLAYLIST` DROP COLUMN `cover_image`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103011818_ApplyFinalErd') THEN

    ALTER TABLE `PLAYLIST` DROP COLUMN `view_count`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103011818_ApplyFinalErd') THEN

    CREATE TABLE `PLAYLIST_VIEW` (
        `view_id` int NOT NULL AUTO_INCREMENT,
        `user_id` int NOT NULL,
        `playlist_id` int NOT NULL,
        `viewed_at` datetime(6) NOT NULL,
        CONSTRAINT `PK_PLAYLIST_VIEW` PRIMARY KEY (`view_id`),
        CONSTRAINT `FK_PLAYLIST_VIEW_PLAYLIST_playlist_id` FOREIGN KEY (`playlist_id`) REFERENCES `PLAYLIST` (`playlist_id`) ON DELETE CASCADE,
        CONSTRAINT `FK_PLAYLIST_VIEW_USER_user_id` FOREIGN KEY (`user_id`) REFERENCES `USER` (`user_id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103011818_ApplyFinalErd') THEN

    CREATE INDEX `IX_PLAYLIST_VIEW_playlist_id` ON `PLAYLIST_VIEW` (`playlist_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103011818_ApplyFinalErd') THEN

    CREATE INDEX `IX_PLAYLIST_VIEW_user_id` ON `PLAYLIST_VIEW` (`user_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103011818_ApplyFinalErd') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261103011818_ApplyFinalErd', '8.0.33');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103012132_AllowAnonymousView') THEN

    ALTER TABLE `PLAYLIST_VIEW` MODIFY COLUMN `user_id` int NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103012132_AllowAnonymousView') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261103012132_AllowAnonymousView', '8.0.33');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103014459_KeepViewsOnUserDelete') THEN

    ALTER TABLE `PLAYLIST_VIEW` DROP FOREIGN KEY `FK_PLAYLIST_VIEW_USER_user_id`;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103014459_KeepViewsOnUserDelete') THEN

    ALTER TABLE `PLAYLIST_VIEW` ADD CONSTRAINT `FK_PLAYLIST_VIEW_USER_user_id` FOREIGN KEY (`user_id`) REFERENCES `USER` (`user_id`) ON DELETE SET NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261103014459_KeepViewsOnUserDelete') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261103014459_KeepViewsOnUserDelete', '8.0.33');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261106061103_AddPlaylistComments') THEN

    CREATE TABLE `PLAYLIST_COMMENT` (
        `comment_id` int NOT NULL AUTO_INCREMENT,
        `user_id` int NULL,
        `playlist_id` int NOT NULL,
        `content` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `created_at` datetime(6) NOT NULL,
        CONSTRAINT `PK_PLAYLIST_COMMENT` PRIMARY KEY (`comment_id`),
        CONSTRAINT `FK_PLAYLIST_COMMENT_PLAYLIST_playlist_id` FOREIGN KEY (`playlist_id`) REFERENCES `PLAYLIST` (`playlist_id`) ON DELETE CASCADE,
        CONSTRAINT `FK_PLAYLIST_COMMENT_USER_user_id` FOREIGN KEY (`user_id`) REFERENCES `USER` (`user_id`) ON DELETE SET NULL
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261106061103_AddPlaylistComments') THEN

    CREATE INDEX `IX_PLAYLIST_COMMENT_playlist_id_created_at` ON `PLAYLIST_COMMENT` (`playlist_id`, `created_at`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261106061103_AddPlaylistComments') THEN

    CREATE INDEX `IX_PLAYLIST_COMMENT_user_id` ON `PLAYLIST_COMMENT` (`user_id`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261106061103_AddPlaylistComments') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261106061103_AddPlaylistComments', '8.0.33');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261106065213_AddPlaylistTags') THEN

    CREATE TABLE `PLAYLIST_TAG` (
        `tag_id` int NOT NULL AUTO_INCREMENT,
        `playlist_id` int NOT NULL,
        `tag_name` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_PLAYLIST_TAG` PRIMARY KEY (`tag_id`),
        CONSTRAINT `FK_PLAYLIST_TAG_PLAYLIST_playlist_id` FOREIGN KEY (`playlist_id`) REFERENCES `PLAYLIST` (`playlist_id`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261106065213_AddPlaylistTags') THEN

    CREATE UNIQUE INDEX `IX_PLAYLIST_TAG_playlist_id_tag_name` ON `PLAYLIST_TAG` (`playlist_id`, `tag_name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261106065213_AddPlaylistTags') THEN

    CREATE INDEX `IX_PLAYLIST_TAG_tag_name` ON `PLAYLIST_TAG` (`tag_name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261106065213_AddPlaylistTags') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261106065213_AddPlaylistTags', '8.0.33');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

