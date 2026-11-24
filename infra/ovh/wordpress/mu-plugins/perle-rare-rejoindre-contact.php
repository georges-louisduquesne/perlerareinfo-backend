<?php
/**
 * Plugin Name: Perle Rare — bouton contact Nous rejoindre
 * Description: Sur la page « Nous rejoindre », remplace chaque « Prenez contact ICI » par un bouton 150×42 qui ouvre le même formulaire.
 */

if (!defined('ABSPATH')) {
    exit;
}

function perle_rare_rejoindre_contact_page($pageId)
{
    return (int) $pageId === 856;
}

function perle_rare_rejoindre_contact_href()
{
    return 'https://perle-rare.typeform.com/to/sbGYNJaj';
}

function perle_rare_rejoindre_contact_label()
{
    return 'Prenez contact ICI';
}

function perle_rare_rejoindre_contact_markup()
{
    return '<p class="pr-rejoindre-contact"><a class="pr-rejoindre-contact__btn" href="'
        . perle_rare_rejoindre_contact_href()
        . '">'
        . perle_rare_rejoindre_contact_label()
        . '</a></p>';
}

/**
 * Anthracite et rose de la charte, comme les boutons d'action du site, calés à 150×42.
 */
function perle_rare_rejoindre_contact_css()
{
    return 'body.page-id-856 .pr-rejoindre-contact{margin:0 0 1em}'
        . 'body.page-id-856 .pr-rejoindre-contact__btn{display:inline-flex !important;align-items:center;justify-content:center;box-sizing:border-box !important;width:150px !important;height:42px !important;min-width:150px !important;max-width:150px !important;min-height:42px !important;max-height:42px !important;padding:0 6px !important;background:#1D242C !important;color:#F6D0D5 !important;border:0 !important;border-radius:0;font-family:"Playfair Display",sans-serif !important;font-size:14px !important;font-weight:400 !important;line-height:1.15 !important;letter-spacing:normal !important;text-align:center;text-decoration:none !important;text-transform:none !important}'
        . 'body.page-id-856 .pr-rejoindre-contact__btn:hover,body.page-id-856 .pr-rejoindre-contact__btn:focus{background:#4F5C6A !important;color:#F6D0D5 !important;text-decoration:none !important}';
}

/**
 * Remplace chaque « Prenez contact ICI » du formulaire. Un second passage ne change plus rien.
 */
function perle_rare_rejoindre_contact_replace($content)
{
    if (!is_string($content) || $content === '') {
        return $content;
    }
    $pattern = '#<p>\s*Prenez contact\s*<a\b[^>]*\bhref\s*=\s*(["\'])https://perle-rare\.typeform\.com/to/sbGYNJaj\1[^>]*>\s*<strong>\s*ICI\s*</strong>\s*</a>\s*</p>#';
    $replaced = preg_replace($pattern, perle_rare_rejoindre_contact_markup(), $content);
    return is_string($replaced) ? $replaced : $content;
}

add_action('wp_head', static function () {
    if (!function_exists('is_page') || !is_page(856)) {
        return;
    }
    echo '<style id="pr-rejoindre-contact">' . perle_rare_rejoindre_contact_css() . "</style>\n";
}, 99);

add_filter('the_content', static function ($content) {
    if (!function_exists('is_page') || !is_page(856)) {
        return $content;
    }
    if (function_exists('in_the_loop') && function_exists('is_main_query') && (!in_the_loop() || !is_main_query())) {
        return $content;
    }
    return perle_rare_rejoindre_contact_replace($content);
}, 30);
