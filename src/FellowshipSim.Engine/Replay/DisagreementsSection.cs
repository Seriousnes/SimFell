using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace FellowshipSim.Engine.Replay;

/// <summary>
/// The review page's Disagreements section: one table per <see cref="IssueCategory"/> with each issue's expected range,
/// observed value and deviation, and the issue's <see cref="EngineState"/> expandable below its row. The states are one
/// JSON block with each aura's name, log ID and modifiers listed once, drawn by the page's script when a row opens.
/// </summary>
internal static class DisagreementsSection
{
    public static void Render(StringBuilder sb, IReadOnlyList<ReplayIssue> issues)
    {
        if (issues.Count == 0) return;
        var states = new List<IssueState>();
        sb.Append($"<h2>Disagreements ({issues.Count:N0})</h2>");
        foreach (var category in Enum.GetValues<IssueCategory>())
        {
            var rows = issues.Where(i => i.Category == category).ToList();
            if (rows.Count == 0) continue;
            sb.Append($"<details class=\"panel\"><summary>{category} ({rows.Count:N0})</summary><div class=\"scroll\"><table class=\"issues\"><thead><tr>");
            sb.Append("<th class=num>Time</th><th class=num>Step</th><th>Subject</th><th class=num>Expected</th><th class=num>Observed</th><th class=num>Deviation</th><th class=num>Relative</th><th></th></tr></thead><tbody>");
            foreach (var issue in rows)
            {
                string unit = issue.Expected?.Unit ?? "";
                sb.Append($"<tr><td class=num>{issue.Time:F3}</td><td class=num><a href=\"#step-{issue.Step}\">{issue.Step}</a></td><td title=\"{EncodeHtml(issue.Message)}\">{EncodeHtml(issue.Subject ?? issue.Message)}</td>");
                sb.Append($"<td class=num>{(issue.Expected is { } expected ? EncodeHtml(expected.ToString()) : "—")}</td>");
                sb.Append($"<td class=num>{(issue.Observed is { } observed ? EncodeHtml(ExpectedRange.Format(observed, unit)) : "—")}</td>");
                sb.Append($"<td class=num>{(issue.Deviation is { } deviation ? EncodeHtml((deviation > 0 ? "+" : "") + ExpectedRange.Format(deviation, unit)) : "—")}</td>");
                sb.Append($"<td class=num>{(issue.RelativeDeviation is { } rel ? rel.ToString("+0.0%;-0.0%;0.0%", CultureInfo.InvariantCulture) : "—")}</td>");
                if (issue.State is { } state)
                {
                    sb.Append($"<td><button class=\"state\" data-s=\"{states.Count}\">Engine state</button></td></tr>");
                    states.Add(new IssueState(issue, state));
                }
                else sb.Append("<td></td></tr>");
            }
            sb.Append("</tbody></table></div></details>");
        }
        sb.Append("<script type=\"application/json\" id=\"engine-states\">");
        sb.Append(StatesJson(states));
        sb.Append("</script><script>");
        sb.Append(Script);
        sb.Append("</script>");
    }

    /// <summary>
    /// <c>{"a": [[name, logId, [modifier, ...]], ...], "s": [state, ...]}</c>. Each state is
    /// <c>[time, message, hero, target, hit, cast]</c>, and each aura in it <c>[aura index, stacks, seconds left]</c>.
    /// </summary>
    private static string StatesJson(List<IssueState> states)
    {
        var auraIndex = new Dictionary<AuraKey, int>();
        var auraList = new List<AuraKey>();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            void Number(double value) => writer.WriteNumberValue(Math.Round(value, 4));
            void NullableNumber(double? value) { if (value is { } present) Number(present); else writer.WriteNullValue(); }
            void Auras(IReadOnlyList<EngineAura> auras)
            {
                writer.WriteStartArray();
                foreach (var aura in auras)
                {
                    var key = new AuraKey(aura.Name, aura.LogId, aura.Modifiers);
                    if (!auraIndex.TryGetValue(key, out int index))
                    {
                        auraIndex[key] = index = auraList.Count;
                        auraList.Add(key);
                    }
                    writer.WriteStartArray();
                    writer.WriteNumberValue(index);
                    writer.WriteNumberValue(aura.Stacks);
                    NullableNumber(aura.SecondsLeft);
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
            }

            writer.WriteStartObject();
            writer.WritePropertyName("s");
            writer.WriteStartArray();
            foreach (var (issue, state) in states)
            {
                writer.WriteStartArray();
                Number(state.Time);
                writer.WriteStringValue(issue.Message);

                var hero = state.Hero;
                writer.WriteStartArray();
                foreach (var value in new[] { hero.PrimaryAttribute, hero.CritChance, hero.CritChanceAgainstTarget, hero.CritMultiplier, hero.Haste, hero.Expertise, hero.Spirit, hero.DamageScale, hero.GlobalCooldownEnd }) Number(value);
                writer.WriteStringValue(string.Join(", ", hero.Resources.Select(x => $"{x.Name} {x.Current.ToString("0.##", CultureInfo.InvariantCulture)}/{x.Max.ToString("0.##", CultureInfo.InvariantCulture)}")));
                writer.WriteStringValue(hero.CurrentCast);
                writer.WriteStringValue(hero.CurrentChannel);
                writer.WriteNumberValue(hero.ActiveEnemies);
                Auras(hero.Auras);
                writer.WriteEndArray();

                if (state.Target is { } target)
                {
                    writer.WriteStartArray();
                    writer.WriteStringValue($"{target.Name} ({target.Id}.{target.InstanceId})");
                    Number(target.HealthFraction);
                    Number(target.IncomingDamageMultiplier);
                    Auras(target.Auras);
                    writer.WriteEndArray();
                }
                else writer.WriteNullValue();

                if (state.Hit is { } hit)
                {
                    writer.WriteStartArray();
                    writer.WriteStringValue($"{hit.AbilityName} ({hit.AbilityId})");
                    Number(hit.BaseDamage);
                    NullableNumber(hit.HitDamageMultiplier);
                    NullableNumber(hit.AbilityDamageMultiplier);
                    Number(hit.TargetDamageMultiplier);
                    Number(hit.CritChance);
                    NullableNumber(hit.CastCritChance);
                    NullableNumber(hit.ExtraCritChance);
                    Number(hit.CritMultiplier);
                    Number(hit.TickFraction);
                    writer.WriteBooleanValue(hit.Snapshot == true);
                    Number(hit.ReleaseTime);
                    NullableNumber(hit.LandingTime);
                    writer.WriteEndArray();
                }
                else writer.WriteNullValue();

                if (state.Cast is { } cast)
                {
                    writer.WriteStartArray();
                    writer.WriteStringValue(cast.Ability);
                    Number(cast.CastTime);
                    Number(cast.GlobalCooldown);
                    writer.WriteBooleanValue(cast.GlobalCooldownScalesWithHaste);
                    Number(cast.CooldownLeft);
                    writer.WriteNumberValue(cast.Charges);
                    Number(cast.CooldownRate);
                    writer.WriteStringValue(cast.Refusal);
                    writer.WriteEndArray();
                }
                else writer.WriteNullValue();
                writer.WriteEndArray();
            }
            writer.WriteEndArray();

            writer.WritePropertyName("a");
            writer.WriteStartArray();
            foreach (var (name, logId, modifiers) in auraList)
            {
                writer.WriteStartArray();
                writer.WriteStringValue(name);
                writer.WriteNumberValue(logId);
                writer.WriteStartArray();
                foreach (var modifier in modifiers) writer.WriteStringValue($"{modifier.Type} {modifier.Op} {modifier.Value.ToString("0.####", CultureInfo.InvariantCulture)}");
                writer.WriteEndArray();
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        // The default encoder escapes '<', so the block cannot close its script element early.
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Opens a row's engine state below it, and closes it again.</summary>
    private const string Script = """
(()=>{const d=JSON.parse(document.getElementById('engine-states').textContent),A=d.a;
const e=s=>String(s??'').replace(/[&<>"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));
const p=v=>(v*100).toFixed(2)+'%',f=v=>v==null?'—':String(v),t=v=>v==null?'never':v.toFixed(3)+'s';
const auras=l=>l.length?l.map(([i,st,left])=>{const[n,id,m]=A[i];return e(n)+(id>0?' ('+id+')':'')+' x'+st+(left==null?'':', '+left.toFixed(3)+'s left')+(m.length?'<span class="muted"> — '+e(m.map(x=>n+' x'+st+': '+x).join('; '))+'</span>':'')}).join('<br>'):'none';
const draw=([time,msg,h,g,x,c])=>{let o='<div class="muted">'+e(msg)+'</div><div class="grid"><div><strong>Hero</strong> <span class="muted">@ '+time.toFixed(3)+'s</span><br>'
+'Primary attribute '+h[0]+' · crit '+p(h[1])+' ('+p(h[2])+' against the target) · crit multiplier '+h[3]+'<br>'
+'Haste '+p(h[4])+' · Expertise '+p(h[5])+' · Spirit '+p(h[6])+' · damage scale '+h[7]+'<br>Resources: '+e(h[9])+'<br>'
+'Global cooldown ends '+h[8].toFixed(3)+'s · casting '+e(h[10]??'nothing')+' · channeling '+e(h[11]??'nothing')+' · '+h[12]+' active enemies<br>Auras:<br>'+auras(h[13])+'</div>';
if(g)o+='<div><strong>Target</strong> '+e(g[0])+'<br>Health '+p(g[1])+' · incoming damage multiplier '+g[2]+'<br>Auras:<br>'+auras(g[3])+'</div>';
if(x)o+='<div><strong>Hit</strong> '+e(x[0])+'<br>Base damage '+x[1].toFixed(1)+' · tick fraction '+x[9]+(x[10]?' · snapshot':'')+'<br>Hit damage multiplier '+f(x[2])+' · ability damage multiplier '+f(x[3])+' · target damage multiplier '+x[4]+'<br>'
+'Crit chance '+p(x[5])+' (cast '+(x[6]==null?'—':p(x[6]))+', hit '+(x[7]==null?'—':p(x[7]))+') · crit multiplier '+x[8]+'<br>Released '+t(x[11])+' · landed '+t(x[12])+'</div>';
if(c)o+='<div><strong>Cast</strong> '+e(c[0])+'<br>Cast time '+c[1].toFixed(3)+'s · global cooldown '+c[2]+'s'+(c[3]?' (hasted)':'')+'<br>Cooldown left '+c[4].toFixed(3)+'s · '+c[5]+' charges · cooldown rate '+c[6]+(c[7]?'<br>Refusal: '+e(c[7]):'')+'</div>';
return o+'</div>'};
document.addEventListener('click',ev=>{const b=ev.target.closest('button.state');if(!b)return;const row=b.closest('tr'),next=row.nextElementSibling;
if(next&&next.classList.contains('state-row')){next.remove();return}
const r=document.createElement('tr');r.className='state-row';r.innerHTML='<td colspan="8">'+draw(d.s[+b.dataset.s])+'</td>';row.after(r)})})();
""";

    private static string EncodeHtml(string text) => WebUtility.HtmlEncode(text);

    private readonly record struct IssueState(ReplayIssue Issue, EngineState State);

    private readonly record struct AuraKey(string Name, int LogId, IReadOnlyList<StatModifier> Modifiers);
}
