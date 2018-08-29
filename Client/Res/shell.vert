#version 330 core
layout(location = 0) in vec3 quadPos;
layout(location = 1) in vec2 shellPos;

uniform mat4 view;
uniform mat4 proj;

out vec4 fCol;

void main()
{
	fCol=vec4(1.0f,0.0f,0.0f,1.0f);
	vec3 pos = quadPos + vec3(shellPos,+0.1f);
	gl_Position=proj*view*vec4(pos,1.0f);
}