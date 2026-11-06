-- My Playlist 샘플 더미 데이터
-- setup.sql을 실행한 뒤 MySQL Workbench에서 전체 실행(Ctrl+Shift+Enter)한다.
-- 여러 번 실행해도 데이터가 중복되지 않는다.
-- 더미 회원은 모두 로그인할 수 있고 비밀번호는 password123이다. 테스트에서만 사용한다.
-- 더미 데이터 전체 삭제: DELETE FROM `USER` WHERE user_name LIKE 'dummy\_%';

USE my_playlist;
SET NAMES utf8mb4;

-- 회원 5명 (user_pw는 PasswordHasher로 만든 해시)
INSERT IGNORE INTO `USER` (user_name, user_pw, created_at) VALUES
    ('dummy_minji', 'AQAAAAIAAYagAAAAEH1f7/ONuXBUMz1IcuDKjRRXLoxtr05gCEuZBHDXdMGFXiU0eR0xzezF+zvXSnfaJA==', '2026-10-01 09:00:00'),
    ('dummy_junho', 'AQAAAAIAAYagAAAAEA5arsKsrlVPr0Qjf+4vL6nNRtkA3ogbsNjBM/t19YqqNETcYhLOyGJbK6W/sTobHg==', '2026-10-02 10:00:00'),
    ('dummy_seoyeon', 'AQAAAAIAAYagAAAAEEHCrIt7LT8yEX/xHyxGX0tXB/lcj4R4oLsD5WMYUDaV+KHF0XATVyztxNF4ddcRag==', '2026-10-03 11:00:00'),
    ('dummy_hyunwoo', 'AQAAAAIAAYagAAAAEKcdkrYfx9bhGGjJTmfkMbN40iyCWsrqepNBJ2Y6jF+sFHBRJSN8VxjW5zOEN5t2dw==', '2026-10-04 12:00:00'),
    ('dummy_jiwoo', 'AQAAAAIAAYagAAAAECBumE2S9Or1WOxL2bKQjt8hhtm6NC3QwgU0XYHaSZzstkUX948NfY4L4Oupw3cDKQ==', '2026-10-05 13:00:00');

-- 플레이리스트 6개 (비공개 1개 포함)
INSERT INTO PLAYLIST (user_id, playlist_title, description, is_public, created_at, updated_at)
SELECT u.user_id, d.title, d.description, d.is_public, d.created_at, d.created_at
FROM (
    SELECT 'dummy_minji' AS user_name, '새벽 감성 노래' AS title, '잠 안 올 때 듣는 노래' AS description, 1 AS is_public, '2026-10-10 22:00:00' AS created_at
    UNION ALL SELECT 'dummy_minji', '운동할 때 듣는 팝', '신나는 노래 모음', 1, '2026-10-11 07:00:00'
    UNION ALL SELECT 'dummy_minji', '혼자 듣는 노래', '비공개 목록', 0, '2026-10-12 23:00:00'
    UNION ALL SELECT 'dummy_junho', '드라이브 플레이리스트', '차에서 듣기 좋은 노래', 1, '2026-10-13 18:00:00'
    UNION ALL SELECT 'dummy_seoyeon', '공부할 때 듣는 노래', '잔잔한 노래 모음', 1, '2026-10-14 20:00:00'
    UNION ALL SELECT 'dummy_hyunwoo', '추억의 팝송', NULL, 1, '2026-10-15 21:00:00'
) d
JOIN `USER` u ON u.user_name = d.user_name
WHERE NOT EXISTS (
    SELECT 1 FROM PLAYLIST p
    WHERE p.user_id = u.user_id AND p.playlist_title = d.title
);

-- 곡 13개 (다른 플레이리스트에 같은 곡이 들어갈 수 있다)
INSERT INTO PLAYLIST_TRACK (playlist_id, youtube_video_id, music_title, artist, youtube_url, sort_order, added_at)
SELECT p.playlist_id, d.video_id, d.music_title, d.artist,
    CONCAT('https://www.youtube.com/watch?v=', d.video_id), d.sort_order, d.added_at
FROM (
    SELECT '새벽 감성 노래' AS title, 1 AS sort_order, 'BzYnNdJhZQw' AS video_id, '밤편지' AS music_title, '아이유' AS artist, '2026-10-10 22:10:00' AS added_at
    UNION ALL SELECT '새벽 감성 노래', 2, '2Vv-BfVoq4g', 'Perfect', 'Ed Sheeran', '2026-10-10 22:11:00'
    UNION ALL SELECT '새벽 감성 노래', 3, 'YQHsXMglC9A', 'Hello', 'Adele', '2026-10-10 22:12:00'
    UNION ALL SELECT '운동할 때 듣는 팝', 1, 'JGwWNGJdvx8', 'Shape of You', 'Ed Sheeran', '2026-10-11 07:10:00'
    UNION ALL SELECT '운동할 때 듣는 팝', 2, 'CevxZvSJLk8', 'Roar', 'Katy Perry', '2026-10-11 07:11:00'
    UNION ALL SELECT '운동할 때 듣는 팝', 3, 'gdZLi9oWNZg', 'Dynamite', 'BTS', '2026-10-11 07:12:00'
    UNION ALL SELECT '혼자 듣는 노래', 1, 'RgKAFK5djSk', 'See You Again', 'Wiz Khalifa', '2026-10-12 23:10:00'
    UNION ALL SELECT '드라이브 플레이리스트', 1, 'hT_nvWreIhg', 'Counting Stars', 'OneRepublic', '2026-10-13 18:10:00'
    UNION ALL SELECT '드라이브 플레이리스트', 2, 'kJQP7kiw5Fk', 'Despacito', 'Luis Fonsi', '2026-10-13 18:11:00'
    UNION ALL SELECT '공부할 때 듣는 노래', 1, 'BzYnNdJhZQw', '밤편지', '아이유', '2026-10-14 20:10:00'
    UNION ALL SELECT '공부할 때 듣는 노래', 2, 'YQHsXMglC9A', 'Hello', 'Adele', '2026-10-14 20:11:00'
    UNION ALL SELECT '추억의 팝송', 1, 'fJ9rUzIMcZQ', 'Bohemian Rhapsody', 'Queen', '2026-10-15 21:10:00'
    UNION ALL SELECT '추억의 팝송', 2, 'dQw4w9WgXcQ', 'Never Gonna Give You Up', 'Rick Astley', '2026-10-15 21:11:00'
) d
JOIN PLAYLIST p ON p.playlist_title = d.title
JOIN `USER` owner_user ON owner_user.user_id = p.user_id AND owner_user.user_name LIKE 'dummy\_%'
WHERE NOT EXISTS (
    SELECT 1 FROM PLAYLIST_TRACK t
    WHERE t.playlist_id = p.playlist_id AND t.sort_order = d.sort_order
);

-- 좋아요 9개 (본인 플레이리스트 좋아요 포함)
INSERT IGNORE INTO PLAYLIST_LIKE (user_id, playlist_id, created_at)
SELECT u.user_id, p.playlist_id, d.created_at
FROM (
    SELECT 'dummy_minji' AS user_name, '새벽 감성 노래' AS title, '2026-10-16 09:00:00' AS created_at
    UNION ALL SELECT 'dummy_junho', '새벽 감성 노래', '2026-10-16 10:00:00'
    UNION ALL SELECT 'dummy_seoyeon', '새벽 감성 노래', '2026-10-16 11:00:00'
    UNION ALL SELECT 'dummy_hyunwoo', '새벽 감성 노래', '2026-10-16 12:00:00'
    UNION ALL SELECT 'dummy_jiwoo', '새벽 감성 노래', '2026-10-16 13:00:00'
    UNION ALL SELECT 'dummy_minji', '드라이브 플레이리스트', '2026-10-17 09:00:00'
    UNION ALL SELECT 'dummy_jiwoo', '드라이브 플레이리스트', '2026-10-17 10:00:00'
    UNION ALL SELECT 'dummy_seoyeon', '운동할 때 듣는 팝', '2026-10-17 11:00:00'
    UNION ALL SELECT 'dummy_jiwoo', '공부할 때 듣는 노래', '2026-10-17 12:00:00'
) d
JOIN `USER` u ON u.user_name = d.user_name
JOIN PLAYLIST p ON p.playlist_title = d.title
JOIN `USER` owner_user ON owner_user.user_id = p.user_id AND owner_user.user_name LIKE 'dummy\_%';

-- 조회 기록 16개 (user_name이 NULL이면 비로그인 조회, 같은 회원의 반복 조회 포함)
INSERT INTO PLAYLIST_VIEW (user_id, playlist_id, viewed_at)
SELECT u.user_id, p.playlist_id, d.viewed_at
FROM (
    SELECT 'dummy_junho' AS user_name, '새벽 감성 노래' AS title, '2026-10-20 09:00:00' AS viewed_at
    UNION ALL SELECT 'dummy_junho', '새벽 감성 노래', '2026-10-20 09:30:00'
    UNION ALL SELECT 'dummy_seoyeon', '새벽 감성 노래', '2026-10-20 10:00:00'
    UNION ALL SELECT NULL, '새벽 감성 노래', '2026-10-20 11:00:00'
    UNION ALL SELECT NULL, '새벽 감성 노래', '2026-10-20 12:00:00'
    UNION ALL SELECT 'dummy_jiwoo', '새벽 감성 노래', '2026-10-20 13:00:00'
    UNION ALL SELECT 'dummy_seoyeon', '운동할 때 듣는 팝', '2026-10-21 09:00:00'
    UNION ALL SELECT NULL, '운동할 때 듣는 팝', '2026-10-21 10:00:00'
    UNION ALL SELECT 'dummy_hyunwoo', '운동할 때 듣는 팝', '2026-10-21 11:00:00'
    UNION ALL SELECT 'dummy_minji', '드라이브 플레이리스트', '2026-10-22 09:00:00'
    UNION ALL SELECT 'dummy_jiwoo', '드라이브 플레이리스트', '2026-10-22 10:00:00'
    UNION ALL SELECT NULL, '드라이브 플레이리스트', '2026-10-22 11:00:00'
    UNION ALL SELECT NULL, '드라이브 플레이리스트', '2026-10-22 12:00:00'
    UNION ALL SELECT 'dummy_jiwoo', '공부할 때 듣는 노래', '2026-10-23 09:00:00'
    UNION ALL SELECT NULL, '추억의 팝송', '2026-10-24 09:00:00'
    UNION ALL SELECT NULL, '추억의 팝송', '2026-10-24 10:00:00'
) d
JOIN PLAYLIST p ON p.playlist_title = d.title
JOIN `USER` owner_user ON owner_user.user_id = p.user_id AND owner_user.user_name LIKE 'dummy\_%'
LEFT JOIN `USER` u ON u.user_name = d.user_name
WHERE NOT EXISTS (
    SELECT 1 FROM PLAYLIST_VIEW v
    WHERE v.playlist_id = p.playlist_id AND v.viewed_at = d.viewed_at AND v.user_id <=> u.user_id
);

-- 확인
SELECT 'USER' AS table_name, COUNT(*) AS row_count FROM `USER` WHERE user_name LIKE 'dummy\_%'
UNION ALL SELECT 'PLAYLIST', COUNT(*) FROM PLAYLIST p JOIN `USER` u ON u.user_id = p.user_id WHERE u.user_name LIKE 'dummy\_%'
UNION ALL SELECT 'PLAYLIST_TRACK', COUNT(*) FROM PLAYLIST_TRACK t JOIN PLAYLIST p ON p.playlist_id = t.playlist_id JOIN `USER` u ON u.user_id = p.user_id WHERE u.user_name LIKE 'dummy\_%'
UNION ALL SELECT 'PLAYLIST_LIKE', COUNT(*) FROM PLAYLIST_LIKE l JOIN PLAYLIST p ON p.playlist_id = l.playlist_id JOIN `USER` u ON u.user_id = p.user_id WHERE u.user_name LIKE 'dummy\_%'
UNION ALL SELECT 'PLAYLIST_VIEW', COUNT(*) FROM PLAYLIST_VIEW v JOIN PLAYLIST p ON p.playlist_id = v.playlist_id JOIN `USER` u ON u.user_id = p.user_id WHERE u.user_name LIKE 'dummy\_%';
