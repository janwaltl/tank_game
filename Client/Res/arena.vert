#version 330 core
layout(location = 0) in vec3 quadPos;
layout(location = 1) in vec3 iPos;
layout(location = 2) in vec3 iCol;

uniform mat4 view;
uniform mat4 proj;

out vec4 fCol;

void main()
{
	fCol=vec4(iCol,1.0f);
	gl_Position=proj*view*vec4(iPos + 0.95*quadPos,1.0f);
}