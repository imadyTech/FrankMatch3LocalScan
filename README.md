## my last manually developed coding project ##

This is my last effort in solving a practical problem with pure human input.

Brute-force like algorithms in match-3 games are widely used to scan the complete board for potential pattern matches. My algorithm uses local-scan instead, only when an unit landed onto the landing position, plus a quick search from pre-calculated hash map.

1. How the pattern is sampled and hashed
Total 8 patterns are pre-defined:
<img width="1836" height="764" alt="image" src="https://github.com/user-attachments/assets/e1362834-4d81-4a80-9723-101b994be5d8" />

and it is observable that only 8 bits are relevant:
<img width="1874" height="861" alt="image" src="https://github.com/user-attachments/assets/5ce0b96e-16a0-447a-b7f3-318d79baab8c" />
Noticeablly the direction of swapping is one of elements that participate the calculation.


2. next step

the algorithm returns the result of match scanning - no match, or the highest matched pattern. Cells will be eliminated, and refilled, and followed by next round match scan when the new units landing to the target cell - one by one, adn the cascading matches could be solved.


3. Now you can try it:

https://imadytech.github.io/FrankMatch3LocalScan/
