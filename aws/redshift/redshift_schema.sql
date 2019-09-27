CREATE TABLE sitecore_page_views
(
	page_view_event_id VARCHAR(36) NOT NULL PRIMARY KEY,
	event_timestamp TIMESTAMPTZ NOT NULL SORTKEY,
	interaction_id VARCHAR(36),
	contact_id VARCHAR(36),
	event_url VARCHAR(256),
	duration_seconds INT, 
	site_name VARCHAR(128),
	email_address VARCHAR(256),
	user_agent VARCHAR(256),
	ip_address VARCHAR(20)
)
DISTKEY(page_view_event_id)