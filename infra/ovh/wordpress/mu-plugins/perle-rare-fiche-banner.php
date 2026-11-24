<?php
/**
 * Plugin Name: Perle Rare — bandeau des fiches conseiller
 * Description: Sur les fiches conseiller, le nom visible devient le seul H1. Le doublon du bandeau reste masqué, mais ce n’est plus un titre.
 */

if (!defined('ABSPATH')) {
    exit;
}

/**
 * Fiche conseiller : le contenu porte le bloc nom-chasseur-immobilier-noir.
 * Les pages liste (nom-chasseur-immobilier, sans -noir) ne sont pas concernées.
 */
function perle_rare_is_fiche_conseiller()
{
    if (!is_singular('page')) {
        return false;
    }
    $post = get_post();
    if (!$post instanceof WP_Post) {
        return false;
    }
    return strpos((string) $post->post_content, 'nom-chasseur-immobilier-noir') !== false;
}

/**
 * Le paragraphe du nom visible devient un H1. Le HTML interne (nom, fonction) ne change pas.
 * Un second passage ne touche plus la page : le bloc est déjà un H1.
 */
function perle_rare_promote_visible_name_heading($html)
{
    if (!is_string($html) || strpos($html, 'nom-chasseur-immobilier-noir') === false) {
        return $html;
    }
    $replaced = preg_replace(
        '/<p(\b[^>]*\bclass\s*=\s*(["\'])[^"\']*\bnom-chasseur-immobilier-noir\b[^"\']*\2[^>]*)>(.*?)<\/p>/is',
        '<h1$1>$3</h1>',
        $html,
        1
    );
    return is_string($replaced) ? $replaced : $html;
}

/**
 * Le H1 du bandeau (nom en doublon, déjà invisible) redevient un paragraphe.
 * Les autres H1 de la page ne sont pas modifiés.
 */
function perle_rare_demote_banner_heading($html)
{
    if (!is_string($html) || strpos($html, 'perle-rare-head') === false) {
        return $html;
    }
    $replaced = preg_replace(
        '/<h1(\b[^>]*\bperle-rare-head\b[^>]*)>(.*?)<\/h1>/is',
        '<p$1>$2</p>',
        $html,
        1
    );
    return is_string($replaced) ? $replaced : $html;
}

function perle_rare_render_fiche_headings($html)
{
    return perle_rare_demote_banner_heading(perle_rare_promote_visible_name_heading($html));
}

/**
 * Le bandeau photo des fiches passe de 188 px à 130 px.
 * La photo garde sa taille d'origine : le bas est rogné, le haut reste visible.
 */
function perle_rare_fiche_banner_css()
{
    return 'body.pr-fiche-conseiller .perle-rare-head{visibility:hidden}'
        . 'body.pr-fiche-conseiller h1.nom-chasseur-immobilier-noir{margin:0 0 11.5px;font-weight:400;font-size:24px;line-height:30px;color:#343434;padding-bottom:30px;font-family:inherit;text-transform:none;letter-spacing:normal}'
        . 'body.pr-fiche-conseiller h1.nom-chasseur-immobilier-noir strong{font-weight:700;text-transform:uppercase}'
        . 'body.pr-fiche-conseiller #content>section:first-of-type:has(.background-media){height:130px;max-height:130px}'
        . 'body.pr-fiche-conseiller #content>section:first-of-type:has(.background-media)>.background-media{height:188px;bottom:auto}';
}

add_filter('body_class', static function ($classes) {
    if (perle_rare_is_fiche_conseiller()) {
        $classes[] = 'pr-fiche-conseiller';
    }
    return $classes;
});

add_action('wp_head', static function () {
    if (!perle_rare_is_fiche_conseiller()) {
        return;
    }
    echo '<style id="pr-fiche-banner">' . perle_rare_fiche_banner_css() . "</style>\n";
}, 99);

add_filter('the_content', static function ($content) {
    if (!perle_rare_is_fiche_conseiller()) {
        return $content;
    }
    return perle_rare_promote_visible_name_heading($content);
}, 20);

add_action('template_redirect', static function () {
    if (!perle_rare_is_fiche_conseiller()) {
        return;
    }
    ob_start('perle_rare_render_fiche_headings');
});
