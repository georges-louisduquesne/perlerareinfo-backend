<?php
/**
 * Plugin Name: Perle Rare — carte virtuelle des fiches
 * Description: Sur les pages personnelles FR et EN : bouton Carte virtuelle, enveloppe mailto, et LinkedIn quand la fiche en a un.
 */

if (!defined('ABSPATH')) {
    exit;
}

/**
 * Clé fr/slug ou en/slug d'une page perle-rare.com. Vide si l'URL n'est pas une page du site.
 */
function perle_rare_fiche_page_key($url)
{
    if (!is_string($url)) {
        return '';
    }
    if (!preg_match('#^https://perle-rare\.com/(fr|en)/([a-z0-9-]+)/?$#i', trim($url), $match)) {
        return '';
    }
    return strtolower($match[1] . '/' . $match[2]);
}

function perle_rare_fiche_carte_href($url)
{
    $key = perle_rare_fiche_page_key($url);
    if ($key === '' || !preg_match('#^(fr|en)/carte-virtuelle-[a-z0-9-]+$#', $key)) {
        return '';
    }
    return 'https://perle-rare.com/' . $key . '/';
}

function perle_rare_fiche_carte_label($english)
{
    return $english ? 'Virtual card' : 'Carte virtuelle';
}

function perle_rare_fiche_email($value)
{
    $email = strtolower(trim((string) $value));
    if ($email === '' || strlen($email) > 255 || !filter_var($email, FILTER_VALIDATE_EMAIL)) {
        return '';
    }
    return $email;
}

function perle_rare_fiche_linkedin_href($url)
{
    $url = trim((string) $url);
    if ($url === '' || strlen($url) > 100 || preg_match('/[\s<>"\']/', $url)) {
        return '';
    }
    $parts = parse_url($url);
    if (!is_array($parts) || empty($parts['scheme']) || empty($parts['host'])) {
        return '';
    }
    if (strtolower((string) $parts['scheme']) !== 'https') {
        return '';
    }
    $host = strtolower((string) $parts['host']);
    if (str_starts_with($host, 'www.')) {
        $host = substr($host, 4);
    }
    return $host === 'linkedin.com' ? $url : '';
}

function perle_rare_fiche_envelope_img()
{
    return '<img class="alignnone size-full wp-image-4359" src="https://perle-rare.com/wp-content/uploads/2021/02/envelope-solid.png" alt="" width="30" height="30" />';
}

function perle_rare_fiche_linkedin_img()
{
    return '<img class="alignnone size-full wp-image-4358" src="https://perle-rare.com/wp-content/uploads/2021/02/linkedin-brands.png" alt="" width="30" height="30" />';
}

function perle_rare_fiche_insert_block($content, $block)
{
    $inColumn = perle_rare_fiche_insert_in_text_column($content, $block);
    return is_string($inColumn) ? $inColumn : $content . $block;
}

/**
 * L'adresse écrite devient l'enveloppe. Le lien est CP_Mel, pas l'alias écrit dans la page.
 * LinkedIn s'ajoute à côté quand la fiche CRM en a un et que la page ne l'affiche pas.
 */
function perle_rare_fiche_present_english_contacts($content, $email, $linkedin)
{
    if (!is_string($content)) {
        return $content;
    }
    $crmEmail = perle_rare_fiche_email($email);
    $linkedin = perle_rare_fiche_linkedin_href($linkedin);
    $mailtoCount = preg_match_all('/<a\b[^>]*mailto:/i', $content);
    if ($crmEmail !== '' && $mailtoCount === 1) {
        $icon = perle_rare_fiche_envelope_img();
        $count = 0;
        $replaced = preg_replace_callback(
            '/<a\b[^>]*\bhref\s*=\s*(["\'])mailto:[^"\']+\1[^>]*>(.*?)<\/a>/is',
            static function ($match) use ($icon, $crmEmail) {
                $href = 'href="mailto:' . htmlspecialchars($crmEmail, ENT_QUOTES) . '"';
                if (stripos($match[0], 'envelope-solid') !== false) {
                    $updated = preg_replace('/\bhref\s*=\s*(["\'])mailto:[^"\']+\1/i', $href, $match[0], 1);
                    return is_string($updated) ? $updated : $match[0];
                }
                return '<a ' . $href . '>' . $icon . '</a>';
            },
            $content,
            1,
            $count
        );
        if (is_string($replaced) && $count === 1) {
            $content = $replaced;
        }
    } elseif ($crmEmail !== '' && $mailtoCount === 0 && stripos($content, 'envelope-solid') === false) {
        $content = perle_rare_fiche_insert_block(
            $content,
            '<p><a href="mailto:' . htmlspecialchars($crmEmail, ENT_QUOTES) . '">' . perle_rare_fiche_envelope_img() . '</a></p>'
        );
    }
    if ($linkedin !== '' && stripos($content, 'linkedin-brands') === false) {
        $link = '<a href="' . htmlspecialchars($linkedin, ENT_QUOTES) . '">' . perle_rare_fiche_linkedin_img() . '</a>';
        $count = 0;
        $with = preg_replace(
            '/(<a\b[^>]*mailto:[^>]*>\s*<img\b[^>]*envelope-solid[^>]*>\s*<\/a>)/i',
            '$1 ' . $link,
            $content,
            1,
            $count
        );
        if (is_string($with) && $count === 1) {
            $content = $with;
        } else {
            $content = perle_rare_fiche_insert_block($content, '<p>' . $link . '</p>');
        }
    }
    return $content;
}

function perle_rare_fiche_carte_button_html($carteUrl, $english)
{
    $href = perle_rare_fiche_carte_href($carteUrl);
    if ($href === '') {
        return '';
    }
    return '<p class="pr-card-launch"><a href="' . htmlspecialchars($href, ENT_QUOTES) . '">'
        . perle_rare_fiche_carte_label($english) . '</a></p>';
}

/**
 * Dernier paragraphe de contact (enveloppe, LinkedIn, Google ou mailto).
 * Le bouton se place juste après, dans la colonne du texte.
 */
function perle_rare_fiche_insert_after_contact_line($content, $button)
{
    if (!preg_match_all('/<p\b[^>]*>.*?<\/p>/is', $content, $matches, PREG_OFFSET_CAPTURE)) {
        return null;
    }
    $end = null;
    foreach ($matches[0] as $match) {
        $html = $match[0];
        if (
            stripos($html, 'mailto:') === false
            && stripos($html, 'envelope-solid') === false
            && stripos($html, 'linkedin-brands') === false
            && stripos($html, 'google-brands') === false
        ) {
            continue;
        }
        $end = $match[1] + strlen($html);
    }
    if ($end === null) {
        return null;
    }
    return substr($content, 0, $end) . $button . substr($content, $end);
}

/**
 * Fiche sans ligne de contact : le bouton reste dans la colonne de texte.
 */
function perle_rare_fiche_insert_in_text_column($content, $button)
{
    if (!preg_match('/<div\b[^>]*\bcol-text\b[^>]*>/i', $content, $match, PREG_OFFSET_CAPTURE)) {
        return null;
    }
    $pos = $match[0][1] + strlen($match[0][0]);
    $depth = 1;
    $length = strlen($content);
    while ($pos < $length && $depth > 0) {
        if (!preg_match('/<\/?div\b[^>]*>/i', $content, $tag, PREG_OFFSET_CAPTURE, $pos)) {
            return null;
        }
        $tagHtml = $tag[0][0];
        $tagAt = $tag[0][1];
        if (stripos($tagHtml, '</div') === 0) {
            $depth--;
            if ($depth === 0) {
                return substr($content, 0, $tagAt) . $button . substr($content, $tagAt);
            }
        } else {
            $depth++;
        }
        $pos = $tagAt + strlen($tagHtml);
    }
    return null;
}

function perle_rare_fiche_with_carte_button($content, $carteUrl, $english)
{
    if (!is_string($content) || stripos($content, 'pr-card-launch') !== false) {
        return $content;
    }
    $button = perle_rare_fiche_carte_button_html($carteUrl, $english);
    if ($button === '') {
        return $content;
    }
    $afterContact = perle_rare_fiche_insert_after_contact_line($content, $button);
    if (is_string($afterContact)) {
        return $afterContact;
    }
    $inColumn = perle_rare_fiche_insert_in_text_column($content, $button);
    if (is_string($inColumn)) {
        return $inColumn;
    }
    return $content . $button;
}

function perle_rare_fiche_carte_launch_css()
{
    return 'body.pr-has-carte .pr-card-launch{margin:18px 0 8px}'
        . 'body.pr-has-carte article>.pr-card-launch{box-sizing:border-box;width:100%;max-width:1170px;margin-left:auto;margin-right:auto;padding-left:15px;padding-right:15px}'
        . 'body.pr-has-carte .pr-card-launch a{display:inline-flex;align-items:center;justify-content:center;min-height:48px;padding:12px 22px;background:#1d242c;color:#ffffff;border:1px solid #1d242c;font-size:0.875rem;font-weight:600;letter-spacing:0.08em;line-height:1.2;text-align:center;text-decoration:none;text-transform:uppercase}'
        . 'body.pr-has-carte .pr-card-launch a:hover,body.pr-has-carte .pr-card-launch a:focus{background:#F6D0D5;border-color:#F6D0D5;color:#1D242C;text-decoration:none}';
}

/**
 * Ligne CRM de la page affichée. Null si l'URL n'est pas une page personnelle connue.
 *
 * @return array{carte:string,email:string,linkedin:string,english:bool}|null
 */
function perle_rare_fiche_carte_lookup($pageUrl)
{
    static $cache = array();
    $key = perle_rare_fiche_page_key($pageUrl);
    if ($key === '' || str_contains($key, '/carte-virtuelle-')) {
        return null;
    }
    if (array_key_exists($key, $cache)) {
        return $cache[$key];
    }
    if (!isset($GLOBALS['wpdb']) || !is_object($GLOBALS['wpdb'])) {
        $cache[$key] = null;
        return null;
    }
    $wpdb = $GLOBALS['wpdb'];
    $sql = 'SELECT CP_CarteVirtuelle AS carte, CP_Mel AS email, CP_LinkedIn AS linkedin,'
        . ' CP_pageWebFr AS web_fr, CP_pageWebEn AS web_en'
        . ' FROM `perle-rareinfo`.conseillers_personnels'
        . ' WHERE CP_pageWebFr LIKE %s OR CP_pageWebEn LIKE %s';
    $like = '%' . $wpdb->esc_like($key) . '%';
    $rows = $wpdb->get_results($wpdb->prepare($sql, $like, $like), ARRAY_A);
    if (!is_array($rows) || $wpdb->last_error) {
        $cache[$key] = null;
        return null;
    }
    $found = null;
    foreach ($rows as $row) {
        $fr = perle_rare_fiche_page_key(isset($row['web_fr']) ? $row['web_fr'] : '');
        $en = perle_rare_fiche_page_key(isset($row['web_en']) ? $row['web_en'] : '');
        if ($fr !== $key && $en !== $key) {
            continue;
        }
        if ($found !== null) {
            $cache[$key] = null;
            return null;
        }
        $found = array(
            'carte' => isset($row['carte']) ? (string) $row['carte'] : '',
            'email' => isset($row['email']) ? (string) $row['email'] : '',
            'linkedin' => isset($row['linkedin']) ? (string) $row['linkedin'] : '',
            'english' => $en === $key,
        );
    }
    $cache[$key] = $found;
    return $found;
}

add_filter('body_class', static function ($classes) {
    if (!function_exists('is_singular') || !is_singular('page') || !function_exists('get_permalink')) {
        return $classes;
    }
    if (perle_rare_fiche_carte_lookup(get_permalink()) !== null) {
        $classes[] = 'pr-has-carte';
    }
    return $classes;
});

add_action('wp_head', static function () {
    if (!function_exists('is_singular') || !is_singular('page') || !function_exists('get_permalink')) {
        return;
    }
    if (perle_rare_fiche_carte_lookup(get_permalink()) === null) {
        return;
    }
    echo '<style id="pr-fiche-carte">' . perle_rare_fiche_carte_launch_css() . "</style>\n";
}, 99);

add_filter('the_content', static function ($content) {
    if (!function_exists('is_singular') || !is_singular('page') || !function_exists('get_permalink')) {
        return $content;
    }
    if (!in_the_loop() || !is_main_query()) {
        return $content;
    }
    $row = perle_rare_fiche_carte_lookup(get_permalink());
    if ($row === null) {
        return $content;
    }
    $content = perle_rare_fiche_present_english_contacts($content, $row['email'], $row['linkedin']);
    return perle_rare_fiche_with_carte_button($content, $row['carte'], !empty($row['english']));
}, 40);
