namespace WeaponPaints.Models;

internal class Knife
{
  public string? knife { get; set; }
  public int weapon_team { get; set; }
  public int? weapon_defindex { get; set; }
}

internal class Agent
{
  public string? agent_ct { get; set; }
  public string? agent_t { get; set; }
}

internal class Skin
{
  public int weapon_defindex { get; set; }
  public int weapon_paint_id { get; set; }
  public float weapon_wear { get; set; }
  public int weapon_seed { get; set; }
  public string? weapon_nametag { get; set; }
  public bool weapon_stattrak { get; set; }
  public int weapon_stattrak_count { get; set; }
  public int weapon_team { get; set; }
  public string? weapon_keychain { get; set; }
  public string? weapon_sticker_0 { get; set; }
  public string? weapon_sticker_1 { get; set; }
  public string? weapon_sticker_2 { get; set; }
  public string? weapon_sticker_3 { get; set; }
  public string? weapon_sticker_4 { get; set; }
}

internal class Music
{
  public int music_id { get; set; }
  public int weapon_team { get; set; }
}

internal class Pin
{
  public int id { get; set; }
  public int weapon_team { get; set; }
}
