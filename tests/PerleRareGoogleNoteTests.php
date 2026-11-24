<?php
/**
 * Lien et lecture de la note Google du pied de page.
 * Usage : php tests/PerleRareGoogleNoteTests.php
 */

define('ABSPATH', __DIR__);

if (!function_exists('add_filter')) {
    function add_filter($hook, $callback, $priority = 10, $accepted_args = 1)
    {
    }
}
if (!function_exists('add_action')) {
    function add_action($hook, $callback, $priority = 10, $accepted_args = 1)
    {
    }
}

require dirname(__DIR__) . '/infra/ovh/wordpress/mu-plugins/perle-rare-google-note.php';

$failures = 0;

function expect_same($label, $actual, $expected)
{
    global $failures;
    if ($actual !== $expected) {
        $failures++;
        fwrite(STDERR, "FAIL $label\n--- actual ---\n" . var_export($actual, true) . "\n--- expected ---\n" . var_export($expected, true) . "\n");
    }
}

$place = perle_rare_google_note_place_id();
$stored = 'https://www.google.com/maps/place//data=!4m7!3m6!1s0x47e66e2dfd101f29:0xa66c69973049351d';
expect_same('stored google url is kept', perle_rare_google_note_url($stored, $place), $stored);
expect_same(
    'foreign url falls back to the vendome place',
    perle_rare_google_note_url('https://evil.example/avis', $place),
    'https://www.google.com/maps/search/?api=1&query=Perle+Rare+Chasseur+Immobilier&query_place_id=' . $place
);
expect_same(
    'javascript url is rejected',
    perle_rare_google_note_url('javascript:alert(1)', $place),
    'https://www.google.com/maps/search/?api=1&query=Perle+Rare+Chasseur+Immobilier&query_place_id=' . $place
);

$badge = '<pre class="ti-widget"><div class="ti-widget ti-goog"><span class="ti-rating">4.8</span>'
    . '<div class="ti-disclaimer"><a href="https://www.trustindex.io/badge">certificat</a></div></div></pre>';
$linked = perle_rare_google_note_html($badge, $stored);
expect_same('badge links to the google listing', strpos($linked, 'href="' . $stored . '"') !== false, true);
expect_same('link opens in a new tab', strpos($linked, 'target="_blank" rel="noopener noreferrer"') !== false, true);
expect_same('disclaimer link stays inside the badge', strpos($linked, 'trustindex.io/badge') !== false, true);
expect_same('second pass does not nest the wrapper', perle_rare_google_note_html($linked, $stored), $linked);
expect_same('other footer text is left alone', perle_rare_google_note_html('<p>Copyright 2026</p>', $stored), '<p>Copyright 2026</p>');

$css = perle_rare_google_note_css();
expect_same('the hit area covers the badge', strpos($css, '.pr-google-note__link{position:absolute;inset:0;z-index:2') !== false, true);
expect_same('the trustindex disclaimer stays clickable', strpos($css, '.ti-disclaimer,.pr-google-note .ti-info-icon{position:relative;z-index:3}') !== false, true);

$parsed = perle_rare_google_rating_from_places_body('{"rating":4.8,"userRatingCount":135}');
expect_same('places body yields 4.8 from 135 reviews', $parsed, array('score' => '4.8', 'count' => 135));
expect_same('a rating above 5 is ignored', perle_rare_google_rating_from_places_body('{"rating":9,"userRatingCount":3}'), null);
expect_same('a body without a rating is ignored', perle_rare_google_rating_from_places_body('{"userRatingCount":135}'), null);
expect_same('empty body is ignored', perle_rare_google_rating_from_places_body(''), null);

$before = array(
    'name' => 'Perle Rare',
    'review_url' => $stored,
    'rating_score' => '4.8',
    'rating_number' => '79',
);
$after = perle_rare_google_note_apply($before, array('score' => '4.9', 'count' => 140));
expect_same('score and count are replaced', $after['rating_score'] === '4.9' && $after['rating_number'] === '140', true);
expect_same('name and review url stay', $after['name'] === 'Perle Rare' && $after['review_url'] === $stored, true);
expect_same('same score is not a change', perle_rare_google_note_changed($before, $before), false);
expect_same('a new score is a change', perle_rare_google_note_changed($before, $after), true);
expect_same(
    'the places call stays on ipv4',
    strpos(file_get_contents(dirname(__DIR__) . '/infra/ovh/wordpress/mu-plugins/perle-rare-google-note.php'), 'CURL_IPRESOLVE_V4') !== false,
    true
);

if ($failures > 0) {
    fwrite(STDERR, "$failures failed\n");
    exit(1);
}
echo "perle-rare-google-note tests passed\n";
