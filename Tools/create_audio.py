"""Synthesize original, gentle game audio; no sampled recordings or music."""
import math, random, struct, wave
from pathlib import Path

out = Path(__file__).resolve().parents[1] / 'Assets' / 'Audio' / 'Generated'
out.mkdir(parents=True, exist_ok=True)
rate = 22050
rng = random.Random(213027)

def save(name, values):
    samples = list(values)
    with wave.open(str(out / (name + '.wav')), 'wb') as file:
        file.setnchannels(1); file.setsampwidth(2); file.setframerate(rate)
        file.writeframes(b''.join(struct.pack('<h', int(max(-.85,min(.85,v))*32767)) for v in samples))

def tone(notes, duration):
    for i in range(int(rate * duration)):
        t = i/rate
        n = min(int(t / (duration/len(notes))), len(notes)-1)
        local = t - n*duration/len(notes)
        envelope = min(1, local/.015) * math.exp(-local*6)
        yield .28 * envelope * (math.sin(2*math.pi*notes[n]*t) + .15*math.sin(2*math.pi*notes[n]*2*t))

save('Correct', tone([523.25,659.25,783.99], .72))
save('Retry', tone([329.63,293.66], .55))
save('Victory', tone([523.25,659.25,783.99,1046.5], 1.8))
save('TimeUp', tone([392,329.63,261.63], 1.1))
save('Switch', tone([900,600], .12))

def water():
    smooth=0
    for i in range(rate*4):
        smooth = .82*smooth + .18*rng.uniform(-1,1)
        yield .35*smooth + .02*math.sin(2*math.pi*850*i/rate)
save('WaterLoop',water())
save('ApplianceLoop',(.05*math.sin(2*math.pi*100*i/rate)+.015*math.sin(2*math.pi*200*i/rate) for i in range(rate*4)))

def music():
    # Frequencies are aligned to the loop period so boundaries remain continuous.
    for i in range(rate*16):
        t=i/rate
        pad=sum(math.sin(2*math.pi*f*t) for f in [130.8125,164.8125,196,261.625])/4
        bell=math.sin(2*math.pi*523.25*t)*(.5+.5*math.sin(2*math.pi*t/8))
        yield .10*pad+.025*bell
save('HomeAmbience',music())
print('Generated', len(list(out.glob('*.wav'))), 'original WAV files.')
