-- My Playlist 쿼리 테스트
-- setup.sql과 seed.sql을 실행한 뒤 사용한다.
-- 각 쿼리를 블록으로 선택하고 Ctrl+Shift+Enter로 실행한 뒤 결과 화면을 캡처한다.
-- 수정과 삭제 테스트는 마지막에 ROLLBACK하므로 데이터가 바뀌지 않는다.

USE my_playlist;
SET NAMES utf8mb4;

-- Q1. DBMS 버전 확인
SELECT VERSION() AS mysql_version;

-- Q2. 테이블 목록 확인
SHOW TABLES;

-- Q3. 테이블 구조 확인
DESC `USER`;
DESC PLAYLIST;
DESC PLAYLIST_TRACK;
DESC PLAYLIST_LIKE;
DESC PLAYLIST_VIEW;

-- Q4. 더미 회원 조회 (비밀번호 제외)
SELECT user_id, user_name, created_at
FROM `USER`
WHERE user_name LIKE 'dummy\_%'
ORDER BY user_id;

-- Q5. 공개 플레이리스트 검색 (제목 또는 아이디에 '노래' 포함)
SELECT p.playlist_id, p.playlist_title, u.user_name, p.is_public
FROM PLAYLIST p
JOIN `USER` u ON u.user_id = p.user_id
WHERE p.is_public = 1
  AND (p.playlist_title LIKE '%노래%' OR u.user_name LIKE '%노래%')
ORDER BY p.created_at DESC;

-- Q6. 인기 플레이리스트 순위 (좋아요 수, 조회수 순)
SELECT p.playlist_title, u.user_name,
    (SELECT COUNT(*) FROM PLAYLIST_LIKE l WHERE l.playlist_id = p.playlist_id) AS like_count,
    (SELECT COUNT(*) FROM PLAYLIST_VIEW v WHERE v.playlist_id = p.playlist_id) AS view_count
FROM PLAYLIST p
JOIN `USER` u ON u.user_id = p.user_id
WHERE p.is_public = 1
ORDER BY like_count DESC, view_count DESC, p.playlist_id DESC
LIMIT 5;

-- Q7. 플레이리스트의 곡 목록 (순서대로)
SELECT t.sort_order, t.music_title, t.artist, t.youtube_url
FROM PLAYLIST_TRACK t
JOIN PLAYLIST p ON p.playlist_id = t.playlist_id
WHERE p.playlist_title = '새벽 감성 노래'
ORDER BY t.sort_order;

-- Q8. 회원이 좋아요한 플레이리스트
SELECT p.playlist_title, owner_user.user_name AS owner_name, l.created_at AS liked_at
FROM PLAYLIST_LIKE l
JOIN `USER` u ON u.user_id = l.user_id
JOIN PLAYLIST p ON p.playlist_id = l.playlist_id
JOIN `USER` owner_user ON owner_user.user_id = p.user_id
WHERE u.user_name = 'dummy_jiwoo'
ORDER BY l.like_id DESC;

-- Q9. 조회 기록 집계 (회원 조회와 비로그인 조회 구분)
SELECT p.playlist_title,
    COUNT(*) AS total_views,
    COUNT(v.user_id) AS member_views,
    SUM(v.user_id IS NULL) AS anonymous_views
FROM PLAYLIST_VIEW v
JOIN PLAYLIST p ON p.playlist_id = v.playlist_id
GROUP BY p.playlist_id, p.playlist_title
ORDER BY total_views DESC, p.playlist_id;

-- Q10. 전체 통계
SELECT
    (SELECT COUNT(*) FROM `USER`) AS user_count,
    (SELECT COUNT(*) FROM PLAYLIST) AS playlist_count,
    (SELECT COUNT(*) FROM PLAYLIST_TRACK) AS track_count,
    (SELECT COUNT(*) FROM PLAYLIST_LIKE) AS like_count,
    (SELECT COUNT(*) FROM PLAYLIST_VIEW) AS view_count;

-- Q11. 플레이리스트 수정 (실행 후 되돌림)
START TRANSACTION;
UPDATE PLAYLIST
SET playlist_title = '새벽 감성 노래 (수정)', is_public = 0
WHERE playlist_title = '새벽 감성 노래';
SELECT playlist_title, is_public
FROM PLAYLIST
WHERE playlist_title LIKE '새벽 감성 노래%';
ROLLBACK;

-- Q12. 플레이리스트 삭제 시 곡, 좋아요, 조회 기록 함께 삭제 (실행 후 되돌림)
START TRANSACTION;
SET @target = (SELECT playlist_id FROM PLAYLIST WHERE playlist_title = '새벽 감성 노래');
SELECT 'before' AS step,
    (SELECT COUNT(*) FROM PLAYLIST_TRACK WHERE playlist_id = @target) AS tracks,
    (SELECT COUNT(*) FROM PLAYLIST_LIKE WHERE playlist_id = @target) AS likes,
    (SELECT COUNT(*) FROM PLAYLIST_VIEW WHERE playlist_id = @target) AS views;
DELETE FROM PLAYLIST WHERE playlist_id = @target;
SELECT 'after' AS step,
    (SELECT COUNT(*) FROM PLAYLIST_TRACK WHERE playlist_id = @target) AS tracks,
    (SELECT COUNT(*) FROM PLAYLIST_LIKE WHERE playlist_id = @target) AS likes,
    (SELECT COUNT(*) FROM PLAYLIST_VIEW WHERE playlist_id = @target) AS views;
ROLLBACK;

-- Q13. 회원탈퇴 시 좋아요는 삭제, 조회 기록은 user_id만 NULL (실행 후 되돌림)
START TRANSACTION;
SET @target_user = (SELECT user_id FROM `USER` WHERE user_name = 'dummy_jiwoo');
SET @member_views = (SELECT COUNT(*) FROM PLAYLIST_VIEW WHERE user_id = @target_user);
SET @total_views = (SELECT COUNT(*) FROM PLAYLIST_VIEW);
SELECT 'before' AS step,
    (SELECT COUNT(*) FROM PLAYLIST_LIKE WHERE user_id = @target_user) AS user_likes,
    @member_views AS user_views,
    @total_views AS total_views;
DELETE FROM `USER` WHERE user_id = @target_user;
SELECT 'after' AS step,
    (SELECT COUNT(*) FROM PLAYLIST_LIKE WHERE user_id = @target_user) AS user_likes,
    (SELECT COUNT(*) FROM PLAYLIST_VIEW WHERE user_id = @target_user) AS user_views,
    (SELECT COUNT(*) FROM PLAYLIST_VIEW) AS total_views;
ROLLBACK;

-- Q14. 중복 아이디 입력 차단 (ERROR 1062가 나와야 정상)
INSERT INTO `USER` (user_name, user_pw, created_at)
VALUES ('dummy_minji', 'x', NOW());

-- Q15. 중복 좋아요 입력 차단 (ERROR 1062가 나와야 정상)
INSERT INTO PLAYLIST_LIKE (user_id, playlist_id, created_at)
SELECT l.user_id, l.playlist_id, NOW()
FROM PLAYLIST_LIKE l
JOIN `USER` u ON u.user_id = l.user_id
WHERE u.user_name = 'dummy_jiwoo'
LIMIT 1;
